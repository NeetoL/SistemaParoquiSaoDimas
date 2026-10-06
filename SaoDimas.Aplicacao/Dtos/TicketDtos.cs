namespace SaoDimas.Aplicacao.Dtos;

public enum TipoFaixaLote
{
    ComResponsavel = 1,
    SemResponsavel = 2,

    /// <summary>Lacuna: números do lote fora de qualquer faixa.</summary>
    Livre = 3
}

public sealed record PreparacaoLoteDto(int EventoId, string Evento, int ProdutoId, string Produto, decimal Preco, int ProximoNumero);

/// <summary>Prévia calculada no servidor (faixa que será reservada e valor potencial), sem gravar.</summary>
public sealed record SimulacaoLoteDto(string? Faixa, int Quantidade, decimal ValorPotencial, string? Erro);

public sealed record FaixaLoteDto(
    int? DistribuicaoId, int NumeroInicial, int NumeroFinal, string Faixa, int Quantidade, string? Responsavel, TipoFaixaLote Tipo,
    PrestacaoDto? Prestacao);

public sealed record LoteDetalhesDto(
    int Id,
    int EventoId,
    string Evento,
    string Comunidade,
    string Produto,
    string Faixa,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorPotencial,
    int Distribuidos,
    int EmFaixasSemResponsavel,
    int Livres,
    int Disponiveis,
    int Responsaveis,
    bool PermiteAlteracoes,
    bool PermitePrestacao,
    IReadOnlyList<FaixaLoteDto> Faixas);

public sealed record DadosDistribuicao(int NumeroInicial, int NumeroFinal, string? Responsavel, string? Operador = null);

public sealed record DadosPrestacao(int Vendidos, int Devolvidos, decimal ValorEntregue, string? Justificativa);

public sealed record PreparacaoPrestacaoDto(
    int EventoId, int LoteId, int DistribuicaoId, string Evento, string Produto, string Faixa, string? Responsavel, int Recebidos,
    decimal PrecoUnitario, DadosPrestacao? Atual);

public sealed record SimulacaoPrestacaoDto(decimal ValorEsperado, decimal Diferenca);

/// <summary>Dados impressos nos tickets. O responsável de cada número é descoberto pela faixa.</summary>
public sealed record ImpressaoTicketsDto(string Comunidade, string Evento, string Produto, decimal Preco, IReadOnlyList<TicketImpressaoDto> Tickets);

public sealed record TicketImpressaoDto(int Numero, string? Responsavel);
