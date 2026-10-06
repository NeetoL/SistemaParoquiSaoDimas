using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class TicketAplicacao(
    IEventoRepositorio eventos,
    ILoteTicketRepositorio lotes,
    IComunidadeRepositorio comunidades,
    IImpressaoTicketService impressao,
    TimeProvider relogio) : ITicketAplicacao
{
    private static readonly Erro NaoEncontrado = Erro.NaoEncontrado("Evento ou lote não encontrado.");

    public async Task<PreparacaoLoteDto?> ObterPreparacaoLoteAsync(int eventoId, int produtoId, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        var produto = evento?.ObterProduto(produtoId);

        return produto is null
            ? null
            : new PreparacaoLoteDto(evento!.Id, evento.Nome, produto.Id, produto.Nome, produto.Preco, produto.ProximoNumeroTicket);
    }

    public async Task<SimulacaoLoteDto> SimularLoteAsync(int eventoId, int produtoId, int quantidade, CancellationToken cancellationToken)
    {
        var produto = (await eventos.ObterPorIdAsync(eventoId, cancellationToken))?.ObterProduto(produtoId);
        if (produto is null)
        {
            return new SimulacaoLoteDto(null, 0, 0, "Produto não encontrado.");
        }

        if (quantidade < 1 || quantidade > LoteTicket.QuantidadeMaxima)
        {
            return new SimulacaoLoteDto(null, 0, 0, $"Informe uma quantidade entre 1 e {LoteTicket.QuantidadeMaxima}.");
        }

        var faixa = FaixaNumeracao.Criar(produto.ProximoNumeroTicket, produto.ProximoNumeroTicket + quantidade - 1);
        return faixa.Sucesso
            ? new SimulacaoLoteDto(faixa.Valor.ToString(), quantidade, quantidade * produto.Preco, null)
            : new SimulacaoLoteDto(null, 0, 0, faixa.Erros[0].Mensagem);
    }

    public async Task<Resultado<int>> GerarLoteAsync(int eventoId, int produtoId, int quantidade, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return Resultado<int>.Falha(NaoEncontrado);
        }

        // Reserva a numeração no evento (próximo número do produto) e cria o lote: gravados juntos.
        var resultado = LoteTicket.Gerar(evento, produtoId, quantidade, relogio.GetUtcNow());
        if (!resultado.Sucesso)
        {
            return Resultado<int>.Falha(resultado.Erros);
        }

        lotes.Adicionar(resultado.Valor);
        await lotes.SalvarAlteracoesAsync(cancellationToken);
        return Resultado<int>.Ok(resultado.Valor.Id);
    }

    public async Task<LoteDetalhesDto?> ObterLoteAsync(int eventoId, int loteId, CancellationToken cancellationToken)
    {
        var (evento, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        if (evento is null || lote is null)
        {
            return null;
        }

        var faixas = lote.Distribuicoes
            .Select(distribuicao => new FaixaLoteDto(
                distribuicao.Id, distribuicao.NumeroInicial, distribuicao.NumeroFinal, distribuicao.Faixa.ToString(), distribuicao.Quantidade,
                distribuicao.Responsavel?.Nome,
                distribuicao.PossuiResponsavel ? TipoFaixaLote.ComResponsavel : TipoFaixaLote.SemResponsavel,
                Mapeamento.Prestacao(distribuicao.Prestacao)))
            .Concat(lote.Lacunas().Select(lacuna => new FaixaLoteDto(
                null, lacuna.Inicial, lacuna.Final, lacuna.ToString(), lacuna.Quantidade, null, TipoFaixaLote.Livre, null)))
            .OrderBy(faixa => faixa.NumeroInicial)
            .ToList();

        return new LoteDetalhesDto(
            lote.Id, evento.Id, evento.Nome, await NomeDaComunidadeAsync(evento.ComunidadeId, cancellationToken),
            evento.ObterProduto(lote.ProdutoEventoId)?.Nome ?? "—", lote.Faixa.ToString(), lote.Quantidade, lote.PrecoUnitario,
            lote.ValorPotencial, lote.QuantidadeDistribuida, lote.QuantidadeEmFaixasSemResponsavel, lote.QuantidadeLivre,
            lote.QuantidadeDisponivel, lote.TotalResponsaveis, evento.PermiteAlteracoes, evento.PermitePrestacaoDeContas, faixas);
    }

    public async Task<DadosDistribuicao?> ObterDistribuicaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken)
    {
        var (_, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        var distribuicao = lote?.ObterDistribuicao(distribuicaoId);

        return distribuicao is null
            ? null
            : new DadosDistribuicao(distribuicao.NumeroInicial, distribuicao.NumeroFinal, distribuicao.Responsavel?.Nome);
    }

    public Task<Resultado> DistribuirAsync(int eventoId, int loteId, DadosDistribuicao dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);
        return AlterarLoteAsync(eventoId, loteId,
            (evento, lote, agora) => lote.Distribuir(evento, dados.NumeroInicial, dados.NumeroFinal, dados.Responsavel, agora, dados.Operador ?? "Sistema"),
            cancellationToken);
    }

    public Task<Resultado> AlterarDistribuicaoAsync(
        int eventoId, int loteId, int distribuicaoId, DadosDistribuicao dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);
        return AlterarLoteAsync(eventoId, loteId,
            (evento, lote, agora) => lote.AlterarDistribuicao(evento, distribuicaoId, dados.NumeroInicial, dados.NumeroFinal, dados.Responsavel, agora, dados.Operador ?? "Sistema"),
            cancellationToken);
    }

    public Task<Resultado> RemoverDistribuicaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken, string? operador = null) =>
        AlterarLoteAsync(eventoId, loteId,
            (evento, lote, agora) => lote.RemoverDistribuicao(evento, distribuicaoId, agora, operador ?? "Sistema"),
            cancellationToken);

    public async Task<PreparacaoPrestacaoDto?> ObterPrestacaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken)
    {
        var (evento, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        var distribuicao = lote?.ObterDistribuicao(distribuicaoId);
        if (evento is null || lote is null || distribuicao is null)
        {
            return null;
        }

        var atual = distribuicao.Prestacao is { } prestacao
            ? new DadosPrestacao(prestacao.Vendidos, prestacao.Devolvidos, prestacao.ValorEntregue, prestacao.Justificativa)
            : null;

        return new PreparacaoPrestacaoDto(
            evento.Id, lote.Id, distribuicao.Id, evento.Nome, evento.ObterProduto(lote.ProdutoEventoId)?.Nome ?? "—",
            distribuicao.Faixa.ToString(), distribuicao.Responsavel?.Nome, distribuicao.Quantidade, lote.PrecoUnitario, atual);
    }

    public async Task<SimulacaoPrestacaoDto?> SimularPrestacaoAsync(
        int eventoId, int loteId, int distribuicaoId, int vendidos, decimal valorEntregue, CancellationToken cancellationToken)
    {
        var (_, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        if (lote?.ObterDistribuicao(distribuicaoId) is null)
        {
            return null;
        }

        var (esperado, diferenca) = PrestacaoContas.Calcular(Math.Max(vendidos, 0), lote.PrecoUnitario, valorEntregue);
        return new SimulacaoPrestacaoDto(esperado, diferenca);
    }

    public Task<Resultado> RegistrarPrestacaoAsync(
        int eventoId, int loteId, int distribuicaoId, DadosPrestacao dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);
        return Task.FromResult(Resultado.Falha(new Erro(string.Empty,
            "Utilize a central de prestação de contas da edição: abra o caixa e registre cada recebimento separadamente.")));
    }

    public async Task<Resultado<ArquivoPdf>> GerarPdfAsync(int eventoId, int loteId, int? distribuicaoId, CancellationToken cancellationToken)
    {
        var (evento, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        if (evento is null || lote is null)
        {
            return Resultado<ArquivoPdf>.Falha(NaoEncontrado);
        }

        var faixa = lote.Faixa;
        if (distribuicaoId is int id)
        {
            var distribuicao = lote.ObterDistribuicao(id);
            if (distribuicao is null)
            {
                return Resultado<ArquivoPdf>.Falha(Erro.NaoEncontrado("Faixa não encontrada."));
            }

            faixa = distribuicao.Faixa;
        }

        // Cada ticket recebe o responsável da faixa que contém o seu número.
        var tickets = Enumerable.Range(faixa.Inicial, faixa.Quantidade)
            .Where(numero => !lote.TicketCancelado(numero))
            .Select(numero => new TicketImpressaoDto(numero, lote.ResponsavelDoTicket(numero)?.Nome))
            .ToList();

        if (tickets.Count == 0) return Resultado<ArquivoPdf>.Falha(new Erro(string.Empty, "Os tickets desta faixa foram cancelados."));

        var produto = evento.ObterProduto(lote.ProdutoEventoId)?.Nome ?? "—";
        var pdf = impressao.Gerar(new ImpressaoTicketsDto(
            await NomeDaComunidadeAsync(evento.ComunidadeId, cancellationToken), evento.Nome, produto, lote.PrecoUnitario, tickets));

        return Resultado<ArquivoPdf>.Ok(new ArquivoPdf(pdf, $"tickets-{Slug(produto)}-{FaixaNumeracao.FormatarNumero(faixa.Inicial)}-{FaixaNumeracao.FormatarNumero(faixa.Final)}.pdf"));
    }

    private async Task<Resultado> AlterarLoteAsync(
        int eventoId, int loteId, Func<Evento, LoteTicket, DateTimeOffset, Resultado> operacao, CancellationToken cancellationToken)
    {
        var (evento, lote) = await ObterAsync(eventoId, loteId, cancellationToken);
        if (evento is null || lote is null)
        {
            return Resultado.Falha(NaoEncontrado);
        }

        var resultado = operacao(evento, lote, relogio.GetUtcNow());
        if (resultado.Sucesso)
        {
            await lotes.SalvarAlteracoesAsync(cancellationToken);
        }

        return resultado;
    }

    /// <summary>
    /// Lote somente se pertencer ao evento informado (impede acessar lotes de outro evento pela rota).
    /// </summary>
    private async Task<(Evento? Evento, LoteTicket? Lote)> ObterAsync(int eventoId, int loteId, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        var lote = evento is null ? null : await lotes.ObterPorIdAsync(loteId, cancellationToken);

        return lote is not null && lote.EventoId == eventoId ? (evento, lote) : (evento, null);
    }

    private async Task<string> NomeDaComunidadeAsync(int comunidadeId, CancellationToken cancellationToken) =>
        (await comunidades.ObterPorIdAsync(comunidadeId, cancellationToken))?.NomeCompleto ?? "—";

    private static string Slug(string texto)
    {
        var semAcentos = new string(texto
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return string.Join('-', Regex.Split(semAcentos.ToLowerInvariant(), "[^a-z0-9]+").Where(parte => parte.Length > 0));
    }
}
