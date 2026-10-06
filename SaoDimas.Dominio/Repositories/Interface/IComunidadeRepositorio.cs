using SaoDimas.Dominio.Entities;

namespace SaoDimas.Dominio.Repositories.Interface;

public interface IComunidadeRepositorio
{
    Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Todas as comunidades (ativas e inativas), na ordem de exibição.
    /// </summary>
    Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken cancellationToken);
}
