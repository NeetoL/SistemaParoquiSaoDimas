using System.Globalization;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio.Repositories.Interface;
namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class RifasAplicacao(IRifasDados dados, IComunidadeRepositorio comunidades, TimeProvider relogio) : IRifasAplicacao
{
    public async Task<IReadOnlyList<RifaDto>> ListarAsync(CancellationToken ct) => (await dados.ListarAsync(ct)).OrderByDescending(r => r.DataSorteio).ThenBy(r => r.Nome).ToArray();
    public async Task<RifaDto?> ObterAsync(Guid id, CancellationToken ct) => (await dados.ListarAsync(ct)).SingleOrDefault(r => r.Id == id);
    public async Task<ResultadoGestao> CriarAsync(DadosRifa d, string operador, CancellationToken ct)
    {
        var premios = d.Premios.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (string.IsNullOrWhiteSpace(d.Nome) || d.Nome.Trim().Length > 150 || premios.Length is < 1 or > 20 || d.Premios.Length > 2000 || d.Quantidade is < 1 or > 10000 || d.Valor <= 0 || d.Valor > 100000 || decimal.Round(d.Valor, 2) != d.Valor || d.DataSorteio < DateOnly.FromDateTime(relogio.GetLocalNow().DateTime)) return new(false, "Confira nome, prêmios, data futura, quantidade (1 a 10.000) e valor com até duas casas decimais.");
        var comunidade = await comunidades.ObterPorIdAsync(d.ComunidadeId, ct); if (comunidade is null || !comunidade.Ativa) return new(false, "Selecione uma comunidade ativa.");
        var r = new RifaDto(Guid.NewGuid(), d.Nome.Trim(), string.Join('\n', premios), d.DataSorteio, d.Valor, d.Quantidade, d.ComunidadeId, "Aberta", 1, [], []);
        await dados.SalvarAsync(r, operador, "Criação de rifa", ct); return new(true, "Rifa criada.", r.Id);
    }
    private static ResultadoGestao? Conferir(RifaDto? r, int revisao, bool aberta = false)
    {
        if (r is null) return new(false, "Rifa não encontrada."); if (r.Revisao != revisao) return new(false, "A rifa foi atualizada por outra pessoa. Recarregue a página.");
        if (r.Situacao is "Cancelada" or "Concluída" || (aberta && r.Situacao != "Aberta")) return new(false, "A situação da rifa não permite esta alteração."); return null;
    }
    private async Task<ResultadoGestao> Salvar(RifaDto r, string operador, string acao, CancellationToken ct) { await dados.SalvarAsync(r with { Revisao = r.Revisao + 1 }, operador, acao, ct); return new(true, acao + " registrada.", r.Id); }
    public async Task<ResultadoGestao> ReservarAsync(Guid id, int revisao, string numeros, string comprador, string telefone, string vendedor, string operador, CancellationToken ct)
    {
        var r = await ObterAsync(id, ct); var erro = Conferir(r, revisao, true); if (erro is not null) return erro;
        var partes = numeros.Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries); var selecionados = new List<int>();
        foreach (var parte in partes) { if (!int.TryParse(parte, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 || n > r!.Quantidade) return new(false, "Informe números de 1 a " + r!.Quantidade.ToString(CultureInfo.InvariantCulture) + ", separados por vírgula."); selecionados.Add(n); }
        if (selecionados.Count is < 1 or > 100 || selecionados.Distinct().Count() != selecionados.Count) return new(false, "Selecione de 1 a 100 números diferentes.");
        if (string.IsNullOrWhiteSpace(comprador) || comprador.Length > 150 || telefone.Length > 40 || vendedor.Length > 150) return new(false, "Informe o comprador e confira os contatos.");
        if (r!.Numeros.Any(n => selecionados.Contains(n.Numero))) return new(false, "Um dos números já está reservado ou pago. Nenhum número foi reservado.");
        var lista = r.Numeros.Concat(selecionados.Select(n => new NumeroRifaDto(n, comprador.Trim(), telefone.Trim(), vendedor.Trim(), false, "", relogio.GetUtcNow().UtcDateTime, null))).OrderBy(n => n.Numero).ToList();
        return await Salvar(r with { Numeros = lista }, operador, "Reserva de números", ct);
    }
    public async Task<ResultadoGestao> PagamentoAsync(Guid id, int revisao, int numero, bool pago, string forma, string operador, CancellationToken ct)
    {
        var r = await ObterAsync(id, ct); var erro = Conferir(r, revisao); if (erro is not null) return erro; var n = r!.Numeros.SingleOrDefault(n => n.Numero == numero);
        if (n is null || n.Pago == pago) return new(false, "Número não encontrado ou pagamento já está nesta situação.");
        if (r.Resultados.Any(x => x.Numero == numero)) return new(false, "O pagamento de um número premiado não pode ser alterado.");
        if (pago && !new[] { "Dinheiro", "PIX", "Transferência", "Cartão", "Outro" }.Contains(forma)) return new(false, "Selecione uma forma de pagamento válida.");
        return await Salvar(r with { Numeros = r.Numeros.Select(x => x.Numero == numero ? x with { Pago = pago, Forma = pago ? forma : "", PagoEm = pago ? relogio.GetUtcNow().UtcDateTime : null } : x).ToList() }, operador, pago ? "Pagamento de número" : "Estorno de pagamento", ct);
    }
    public async Task<ResultadoGestao> LiberarAsync(Guid id, int revisao, int numero, string operador, CancellationToken ct)
    {
        var r = await ObterAsync(id, ct); var erro = Conferir(r, revisao, true); if (erro is not null) return erro; var n = r!.Numeros.SingleOrDefault(n => n.Numero == numero);
        if (n is null || n.Pago) return new(false, "Só é possível liberar uma reserva não paga. Estorne o pagamento primeiro.");
        return await Salvar(r with { Numeros = r.Numeros.Where(x => x.Numero != numero).ToList() }, operador, "Liberação de reserva", ct);
    }
    public async Task<ResultadoGestao> SituacaoAsync(Guid id, int revisao, string situacao, string operador, CancellationToken ct)
    {
        var r = await ObterAsync(id, ct); var erro = Conferir(r, revisao); if (erro is not null) return erro;
        if (situacao == r!.Situacao || situacao is not ("Aberta" or "Fechada" or "Cancelada")) return new(false, "Situação inválida.");
        if (situacao == "Cancelada" && r.Numeros.Any(n => n.Pago)) return new(false, "Estorne os pagamentos antes de cancelar a rifa.");
        if (r.Resultados.Count > 0) return new(false, "Uma rifa com resultado não pode ser reaberta ou cancelada.");
        return await Salvar(r with { Situacao = situacao }, operador, "Situação da rifa", ct);
    }
    public async Task<ResultadoGestao> ResultadoAsync(Guid id, int revisao, int premio, int numero, string referencia, string operador, CancellationToken ct)
    {
        var r = await ObterAsync(id, ct); var erro = Conferir(r, revisao); if (erro is not null) return erro;
        if (r!.Situacao != "Fechada" || premio < 1 || premio > r.Premios.Split('\n').Length || r.Resultados.Any(x => x.Premio == premio) || string.IsNullOrWhiteSpace(referencia) || referencia.Length > 500) return new(false, "Feche as vendas, selecione um prêmio sem resultado e informe a referência do sorteio.");
        var n = r.Numeros.SingleOrDefault(x => x.Numero == numero); if (n is null || !n.Pago) return new(false, "O número vencedor precisa estar pago.");
        var resultados = r.Resultados.Append(new ResultadoRifaDto(premio, numero, n.Comprador, referencia.Trim(), relogio.GetUtcNow().UtcDateTime)).ToList();
        return await Salvar(r with { Resultados = resultados, Situacao = resultados.Count == r.Premios.Split('\n').Length ? "Concluída" : "Fechada" }, operador, "Resultado do sorteio", ct);
    }
}
