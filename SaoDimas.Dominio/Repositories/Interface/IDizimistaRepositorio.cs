using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Dominio.Repositories.Interface;

public interface IDizimistaRepositorio
{
    Task<Dizimista?> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExisteCpfAsync(Cpf cpf, int? ignorarDizimistaId, CancellationToken cancellationToken);

    void Adicionar(Dizimista dizimista);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
