using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Dominio.ValueObjects;
using SaoDimas.Infraestrutura.Persistencia.Json;
namespace SaoDimas.Infraestrutura.Repositories.Implementation;
internal sealed class DizimistaJsonRepositorio(SessaoSistemaJson sessao) : IDizimistaRepositorio
{
    public async Task<Dizimista?> ObterPorIdAsync(int id, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return sessao.Dizimistas.FirstOrDefault(d => d.Id == id);
    }
    public async Task<bool> ExisteCpfAsync(Cpf cpf, int? ignorarDizimistaId, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return sessao.Dizimistas.Any(d => d.Id != ignorarDizimistaId && d.Cpf == cpf);
    }
    public void Adicionar(Dizimista dizimista) => sessao.Dizimistas.Add(dizimista);
    public Task SalvarAlteracoesAsync(CancellationToken ct) => sessao.SalvarAsync(ct);
}
