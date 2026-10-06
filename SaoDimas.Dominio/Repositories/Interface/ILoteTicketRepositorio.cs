using SaoDimas.Dominio.Entities;

namespace SaoDimas.Dominio.Repositories.Interface;

public interface ILoteTicketRepositorio
{
    Task<LoteTicket?> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoteTicket>> ListarPorEventoAsync(int eventoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoteTicket>> ListarTodosAsync(CancellationToken cancellationToken);

    void Adicionar(LoteTicket lote);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
