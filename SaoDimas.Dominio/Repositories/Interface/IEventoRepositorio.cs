using SaoDimas.Dominio.Entities;

namespace SaoDimas.Dominio.Repositories.Interface;

public interface IEventoRepositorio
{
    Task<Evento?> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Evento>> ListarAsync(CancellationToken cancellationToken);

    void Adicionar(Evento evento);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
