using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.MVC.Models;

public sealed record DizimistasIndexViewModel(
    FiltroDizimistas Filtro,
    PaginaResultado<DizimistaResumoDto> Resultado,
    IReadOnlyList<ComunidadeDto> Comunidades)
{
    /// <summary>
    /// Valores de rota da listagem atual, para links de paginação e ordenação que preservam os filtros.
    /// </summary>
    public object Rota(int? pagina = null, OrdenacaoDizimistas? ordenacao = null, bool? descendente = null) => new
    {
        busca = Filtro.Busca,
        comunidadeId = Filtro.ComunidadeId,
        ordenacao = ordenacao ?? Filtro.Ordenacao,
        descendente = descendente ?? Filtro.Descendente,
        pagina = pagina ?? Resultado.Pagina,
        tamanhoPagina = Filtro.TamanhoPagina == FiltroDizimistas.TamanhoPaginaPadrao ? (int?)null : Filtro.TamanhoPagina
    };

    /// <summary>
    /// Link de ordenação de uma coluna: alterna asc/desc na coluna atual e volta à primeira página.
    /// </summary>
    public object RotaOrdenacao(OrdenacaoDizimistas coluna) =>
        Rota(pagina: 1, ordenacao: coluna, descendente: Filtro.Ordenacao == coluna && !Filtro.Descendente);

    public string? AriaSort(OrdenacaoDizimistas coluna) =>
        Filtro.Ordenacao != coluna ? null : Filtro.Descendente ? "descending" : "ascending";
}
