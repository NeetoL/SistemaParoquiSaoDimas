namespace SaoDimas.Aplicacao.Dtos;

public sealed record PaginaResultado<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int Total)
{
    public int TotalPaginas => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)TamanhoPagina);

    public bool TemAnterior => Pagina > 1;

    public bool TemProxima => Pagina < TotalPaginas;

    public int PrimeiroItem => Total == 0 ? 0 : ((Pagina - 1) * TamanhoPagina) + 1;

    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, Total);
}
