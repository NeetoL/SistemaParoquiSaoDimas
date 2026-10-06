using SaoDimas.Dominio.Enums;

namespace SaoDimas.Aplicacao.Dtos;

public enum AcaoStatusEvento
{
    Ativar = 1,
    Encerrar = 2,
    Cancelar = 3
}

/// <summary>
/// Pesquisa e filtros da tela de eventos (comunidade, status e período).
/// </summary>
public sealed record FiltroEventos
{
    public string? Busca { get; init; }

    public int? ComunidadeId { get; init; }

    public StatusEvento? Status { get; init; }

    /// <summary>Eventos que acontecem (total ou parcialmente) a partir desta data.</summary>
    public DateOnly? De { get; init; }

    /// <summary>Eventos que acontecem (total ou parcialmente) até esta data.</summary>
    public DateOnly? Ate { get; init; }

    public FiltroEventos Normalizado() => this with
    {
        Busca = string.IsNullOrWhiteSpace(Busca) ? null : Busca.Trim(),
        ComunidadeId = ComunidadeId > 0 ? ComunidadeId : null,
        Status = Status is { } status && Enum.IsDefined(status) ? status : null
    };
}

public sealed record DadosEvento(string Nome, string? Descricao, int ComunidadeId, DateOnly DataInicio, DateOnly? DataFim, string? Observacoes, int? ModeloEdicaoId = null, string? Operador = null);

public sealed record DadosProduto(string Nome, string? Descricao, decimal Preco, bool Ativo, string? Operador = null);

public sealed record IndicadoresEventosDto(int EventosAtivos, int Produtos, int TicketsGerados, int TicketsDistribuidos);

public sealed record EventoResumoDto(
    int Id, string Nome, string Comunidade, DateOnly DataInicio, DateOnly? DataFim, int Produtos, StatusEvento Status, bool PermiteAlteracoes)
{
    public Guid EventoBaseId { get; init; }
    public string NomeBase { get; init; } = Nome;
    public int Ano { get; init; } = DataInicio.Year;
    public int TotalEdicoes { get; init; } = 1;
}

public sealed record PainelEventosDto(IndicadoresEventosDto Indicadores, IReadOnlyList<EventoResumoDto> Eventos);

public sealed record ProdutoEventoDto(int Id, string Nome, string? Descricao, decimal Preco, bool Ativo, int TicketsGerados, int ProximoNumero);

public sealed record LoteResumoDto(
    int Id, int ProdutoId, string Produto, string Faixa, int Quantidade, decimal PrecoUnitario, decimal ValorPotencial,
    int Distribuidos, int Disponiveis, int Responsaveis);

/// <summary>Uma pessoa com todas as suas faixas no evento (pode ter várias, em vários produtos).</summary>
public sealed record ResponsavelResumoDto(string Nome, IReadOnlyList<string> Faixas, int Tickets, int PrestacoesPendentes);

public sealed record PrestacaoDto(
    int Recebidos, int Vendidos, int Devolvidos, decimal PrecoUnitario, decimal ValorEsperado, decimal ValorEntregue,
    decimal Diferenca, string? Justificativa, DateTime RegistradaEm);

public sealed record PrestacaoResumoDto(
    int LoteId, int DistribuicaoId, string Produto, string Faixa, string Responsavel, int Recebidos, decimal PrecoUnitario, PrestacaoDto? Prestacao);

public sealed record TotaisPrestacaoDto(decimal ValorEsperado, decimal ValorEntregue, decimal Diferenca, int FaixasPrestadas, int FaixasPendentes);

public sealed record EventoDetalhesDto(
    int Id,
    string Nome,
    string? Descricao,
    int ComunidadeId,
    string Comunidade,
    DateOnly DataInicio,
    DateOnly? DataFim,
    StatusEvento Status,
    string? Observacoes,
    bool PermiteAlteracoes,
    bool PermitePrestacao,
    int TicketsGerados,
    int TicketsDistribuidos,
    int TicketsDisponiveis,
    IReadOnlyList<ProdutoEventoDto> Produtos,
    IReadOnlyList<LoteResumoDto> Lotes,
    IReadOnlyList<ResponsavelResumoDto> Responsaveis,
    IReadOnlyList<PrestacaoResumoDto> Prestacoes,
    TotaisPrestacaoDto TotaisPrestacao);
