using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Aplicacao.Services.Interface;

public interface IComunidadeAplicacao
{
    /// <summary>
    /// Todas as comunidades (ex.: filtros de listagens e relatórios).
    /// </summary>
    Task<IReadOnlyList<ComunidadeDto>> ListarTodasAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Comunidades que podem receber vínculo, mais a comunidade atual (mesmo se inativa) na edição.
    /// </summary>
    Task<IReadOnlyList<ComunidadeDto>> ListarParaVinculoAsync(int? comunidadeAtualId, CancellationToken cancellationToken);
}
