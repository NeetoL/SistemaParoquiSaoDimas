namespace SaoDimas.Aplicacao.Dtos;

public enum OrdenacaoDizimistas
{
    Nome = 1,
    Codigo = 2,
    Comunidade = 3
}

/// <summary>
/// Pesquisa, filtro, ordenação e paginação da listagem de dizimistas (aplicados no servidor).
/// </summary>
public sealed record FiltroDizimistas
{
    public const int TamanhoPaginaPadrao = 20;
    public const int TamanhoPaginaMaximo = 100;

    /// <summary>
    /// Nome, código, telefone ou CPF.
    /// </summary>
    public string? Busca { get; init; }

    public int? ComunidadeId { get; init; }

    public OrdenacaoDizimistas Ordenacao { get; init; } = OrdenacaoDizimistas.Nome;

    public bool Descendente { get; init; }

    public int Pagina { get; init; } = 1;

    public int TamanhoPagina { get; init; } = TamanhoPaginaPadrao;

    /// <summary>
    /// Corrige valores fora dos limites recebidos do navegador.
    /// </summary>
    public FiltroDizimistas Normalizado() => this with
    {
        Busca = string.IsNullOrWhiteSpace(Busca) ? null : Busca.Trim(),
        ComunidadeId = ComunidadeId > 0 ? ComunidadeId : null,
        Ordenacao = Enum.IsDefined(Ordenacao) ? Ordenacao : OrdenacaoDizimistas.Nome,
        Pagina = Math.Max(Pagina, 1),
        TamanhoPagina = Math.Clamp(TamanhoPagina, 1, TamanhoPaginaMaximo)
    };
}
