using Microsoft.EntityFrameworkCore;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Dominio.ValueObjects;
using SaoDimas.Infraestrutura.Persistencia;

namespace SaoDimas.Infraestrutura.Repositories.Implementation;

internal sealed class DizimistaRepositorio(SaoDimasDbContext contexto) : IDizimistaRepositorio
{
    public Task<Dizimista?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Dizimistas.FirstOrDefaultAsync(dizimista => dizimista.Id == id, cancellationToken);

    public Task<bool> ExisteCpfAsync(Cpf cpf, int? ignorarDizimistaId, CancellationToken cancellationToken) =>
        contexto.Dizimistas.AnyAsync(
            dizimista => dizimista.Cpf == cpf && dizimista.Id != ignorarDizimistaId,
            cancellationToken);

    public void Adicionar(Dizimista dizimista) => contexto.Dizimistas.Add(dizimista);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => contexto.SaveChangesAsync(cancellationToken);
}
