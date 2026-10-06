using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;

namespace SaoDimas.Infraestrutura.Repositories.Implementation;

/// <summary>
/// Eventos na persistência temporária do navegador. Na migração para SQL Server, esta implementação passa a usar
/// o SaoDimasDbContext (o contrato e os casos de uso permanecem iguais).
/// </summary>
internal sealed class EventoRepositorio(EstadoEventosNavegador estado) : IEventoRepositorio
{
    public Task<Evento?> ObterPorIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(estado.ObterEvento(id));

    public Task<IReadOnlyList<Evento>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult(estado.ListarEventos());

    public void Adicionar(Evento evento) => estado.Adicionar(evento);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        estado.Salvar();
        return Task.CompletedTask;
    }
}
