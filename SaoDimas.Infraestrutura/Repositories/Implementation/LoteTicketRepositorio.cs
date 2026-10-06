using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;

namespace SaoDimas.Infraestrutura.Repositories.Implementation;

/// <summary>
/// Lotes de tickets (com faixas e prestações) na persistência temporária do navegador.
/// Na migração para SQL Server, esta implementação passa a usar o SaoDimasDbContext.
/// </summary>
internal sealed class LoteTicketRepositorio(EstadoEventosNavegador estado) : ILoteTicketRepositorio
{
    public Task<LoteTicket?> ObterPorIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(estado.ObterLote(id));

    public Task<IReadOnlyList<LoteTicket>> ListarPorEventoAsync(int eventoId, CancellationToken cancellationToken) =>
        Task.FromResult(estado.ListarLotes(lote => lote.EventoId == eventoId));

    public Task<IReadOnlyList<LoteTicket>> ListarTodosAsync(CancellationToken cancellationToken) =>
        Task.FromResult(estado.ListarLotes(_ => true));

    public void Adicionar(LoteTicket lote) => estado.Adicionar(lote);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        estado.Salvar();
        return Task.CompletedTask;
    }
}
