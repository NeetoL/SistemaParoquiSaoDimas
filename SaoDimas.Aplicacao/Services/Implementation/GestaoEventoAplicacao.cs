using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class GestaoEventoAplicacao(IEventoRepositorio eventos, ILoteTicketRepositorio lotes,
    IComunidadeRepositorio comunidades, TimeProvider relogio) : IGestaoEventoAplicacao
{
    public async Task<GestaoEventoDto?> ObterAsync(int id, CancellationToken ct)
    {
        var evento = await eventos.ObterPorIdAsync(id, ct);
        if (evento is null) return null;
        var registros = await lotes.ListarPorEventoAsync(id, ct);
        var contas = Contas(evento, registros);
        var todos = await eventos.ListarAsync(ct);
        var produtos = evento.Produtos.Select(p =>
        {
            var ls = registros.Where(l => l.ProdutoEventoId == p.Id).ToList();
            return new ResultadoProdutoDto(p.Nome, ls.Sum(l => l.Quantidade),
                ls.Sum(l => l.Distribuicoes.Sum(f => f.Prestacao?.Vendidos ?? 0)),
                ls.Sum(l => l.Distribuicoes.Sum(f => f.Prestacao?.Devolvidos ?? 0)),
                ls.Sum(l => l.Distribuicoes.Sum(f => f.Prestacao?.ValorEsperado ?? 0m)), ls.Sum(l => l.QuantidadeCancelada));
        }).ToList();
        return new(id, evento.Base, evento.Ano, (await comunidades.ObterPorIdAsync(evento.ComunidadeId, ct))?.NomeCompleto ?? "—",
            evento.DataInicio, evento.DataFim, todos.Where(e => e.Base.Id == evento.Base.Id).OrderByDescending(e => e.Ano)
                .Select(e => new EdicaoResumoDto(e.Id, e.Ano, e.DataInicio, e.DataFim, e.Status, e.Caixa.Situacao)).ToList(),
            evento.Caixa.Exportar(), evento.Caixa.Situacao, contas, produtos, registros.Sum(l => l.ValorPotencial),
            contas.Sum(c => c.Devido), contas.Sum(c => c.Prestado), evento.Caixa.DinheiroTeorico,
            Pendencias(evento, registros, contas), evento.PermitePrestacaoDeContas)
            { Disponiveis = registros.SelectMany(l => l.Lacunas().Select(f => new DisponibilidadeTicketDto(l.Id,
                evento.ObterProduto(l.ProdutoEventoId)?.Nome ?? "—", f.Inicial, f.Final))).ToList() };
    }

    private static List<ContaResponsavelDto> Contas(Evento evento, IReadOnlyList<LoteTicket> registros) => registros
        .SelectMany(l => l.Distribuicoes.Where(f => f.PossuiResponsavel).Select(f => (Lote: l, Faixa: f)))
        .GroupBy(x => x.Faixa.Responsavel!.Nome, StringComparer.OrdinalIgnoreCase)
        .OrderBy(g => g.Key).Select(g => new ContaResponsavelDto(g.Key, g.Select(x => new FaixaPrestacaoDto(x.Lote.Id,
            x.Faixa.Id, evento.ObterProduto(x.Lote.ProdutoEventoId)?.Nome ?? "—", x.Faixa.Faixa.ToString(), x.Faixa.Quantidade,
            x.Faixa.Prestacao?.Vendidos, x.Faixa.Prestacao?.Devolvidos, x.Lote.PrecoUnitario)).ToList(),
            g.Sum(x => x.Faixa.Prestacao?.ValorEsperado ?? 0m), evento.Caixa.Prestado(g.Key) +
            (evento.Caixa.Abertura is null ? g.Sum(x => x.Faixa.Prestacao?.ValorEntregue ?? 0m) : 0m))).ToList();

    private static List<string> Pendencias(Evento evento, IReadOnlyList<LoteTicket> registros, IReadOnlyList<ContaResponsavelDto> contas)
    {
        var pendencias = contas.Where(c => c.Pendente != 0m).Select(c => $"{c.Nome}: saldo {c.Pendente:C2}.").ToList();
        var semSituacao = registros.Sum(l => l.Quantidade - l.QuantidadeCancelada - l.Distribuicoes.Sum(f => (f.Prestacao?.Vendidos ?? 0) + (f.Prestacao?.Devolvidos ?? 0)));
        if (semSituacao > 0) pendencias.Add($"{semSituacao} tickets sem situação confirmada (inclui tickets não distribuídos).");
        return pendencias;
    }

    public async Task<Resultado<int>> NovaEdicaoAsync(int id, int ano, DateOnly inicio, DateOnly? fim, bool produtos, bool precos, CancellationToken ct, string? operador = null)
    {
        var origem = await eventos.ObterPorIdAsync(id, ct);
        if (origem is null) return Resultado<int>.Falha(Erro.NaoEncontrado("Edição não encontrada."));
        if ((await eventos.ListarAsync(ct)).Any(e => e.Base.Id == origem.Base.Id && e.Ano == ano))
            return Resultado<int>.Falha(new Erro("Ano", "Este evento já possui uma edição neste ano."));
        var nova = origem.CriarEdicao(ano, inicio, fim, produtos, precos, relogio.GetUtcNow());
        if (!nova.Sucesso) return Resultado<int>.Falha(nova.Erros);
        eventos.Adicionar(nova.Valor);
        nova.Valor.Caixa.Auditar(operador ?? "Sistema", "Nova edição", $"Modelo: {origem.Id}/{origem.Ano}", $"Edição {ano}; financeiro e operação zerados", relogio.GetUtcNow());
        await eventos.SalvarAlteracoesAsync(ct);
        return Resultado<int>.Ok(nova.Valor.Id);
    }

    public Task<Resultado> AbrirAsync(int id, decimal troco, string operador, string? observacao, CancellationToken ct) =>
        Alterar(id, async e =>
        {
            var resultado = e.Caixa.Abrir(troco, operador, observacao, relogio.GetUtcNow());
            if (!resultado.Sucesso) return resultado;
            foreach (var lote in await lotes.ListarPorEventoAsync(id, ct))
                foreach (var faixa in lote.Distribuicoes.Where(f => f.Prestacao?.ValorEntregue > 0m))
                    e.Caixa.ImportarPrestacaoLegada(faixa.Responsavel!.Nome, faixa.Prestacao!.ValorEntregue,
                        faixa.Prestacao.RegistradaEm, $"Lote {lote.Id}, faixa {faixa.Id}");
            return resultado;
        }, ct);

    public Task<Resultado> SituacaoAsync(int id, int loteId, int faixaId, int vendidos, int devolvidos, string operador, CancellationToken ct) =>
        Alterar(id, async e =>
        {
            var lote = await lotes.ObterPorIdAsync(loteId, ct);
            return lote is null || lote.EventoId != id ? Resultado.Falha(Erro.NaoEncontrado("Lote não encontrado nesta edição."))
                : lote.RegistrarSituacao(e, faixaId, vendidos, devolvidos, operador, relogio.GetUtcNow());
        }, ct);

    public Task<Resultado> CancelarTicketsAsync(int id, int loteId, int inicial, int final, string motivo, string operador, CancellationToken ct) =>
        Alterar(id, async e =>
        {
            var lote = await lotes.ObterPorIdAsync(loteId, ct);
            return lote is null || lote.EventoId != id ? Resultado.Falha(Erro.NaoEncontrado("Lote não encontrado nesta edição."))
                : lote.CancelarTickets(e, inicial, final, motivo, operador, relogio.GetUtcNow());
        }, ct);

    public Task<Resultado> ReceberAsync(int id, string responsavel, decimal valor, FormaRecebimento forma, string operador, string? observacao, CancellationToken ct, Guid? requisicaoId = null) =>
        Alterar(id, async e =>
        {
            var conta = Contas(e, await lotes.ListarPorEventoAsync(id, ct)).FirstOrDefault(c => string.Equals(c.Nome, responsavel, StringComparison.OrdinalIgnoreCase));
            return conta is null ? Resultado.Falha(Erro.NaoEncontrado("Responsável não encontrado nesta edição."))
                : e.Caixa.Receber(conta.Nome, valor, conta.Devido, forma, operador, observacao, relogio.GetUtcNow(), requisicaoId);
        }, ct);

    public Task<Resultado> EstornarAsync(int id, Guid lancamento, string motivo, string operador, CancellationToken ct) =>
        Alterar(id, e => Task.FromResult(e.Caixa.Estornar(lancamento, motivo, operador, relogio.GetUtcNow())), ct);
    public Task<Resultado> ConferirAsync(int id, decimal contado, string? justificativa, string operador, CancellationToken ct) =>
        Alterar(id, e => Task.FromResult(e.Caixa.Conferir(contado, justificativa, operador, relogio.GetUtcNow())), ct);
    public Task<Resultado> FecharAsync(int id, bool excepcional, string? motivo, string? justificativa, string operador, CancellationToken ct) =>
        Alterar(id, async e =>
        {
            var registros = await lotes.ListarPorEventoAsync(id, ct);
            var comunidade = (await comunidades.ObterPorIdAsync(e.ComunidadeId, ct))?.NomeCompleto ?? "—";
            var identidade = new IdentidadeFechamento(e.Nome, e.Ano, comunidade, e.DataInicio, e.DataFim);
            var resultado = e.Caixa.Fechar(Pendencias(e, registros, Contas(e, registros)), excepcional, motivo, justificativa, operador, relogio.GetUtcNow(), identidade);
            if (resultado.Sucesso) e.ConfirmarFechamento(relogio.GetUtcNow());
            return resultado;
        }, ct);
    private async Task<Resultado> Alterar(int id, Func<Evento, Task<Resultado>> operacao, CancellationToken ct)
    {
        var evento = await eventos.ObterPorIdAsync(id, ct);
        if (evento is null) return Resultado.Falha(Erro.NaoEncontrado("Edição não encontrada."));
        if (!evento.PermitePrestacaoDeContas) return Resultado.Falha(new Erro(string.Empty, "A edição está bloqueada."));
        var resultado = await operacao(evento);
        if (resultado.Sucesso) await eventos.SalvarAlteracoesAsync(ct);
        return resultado;
    }
}
