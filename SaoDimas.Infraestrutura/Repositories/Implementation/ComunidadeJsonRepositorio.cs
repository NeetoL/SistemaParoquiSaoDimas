using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura.Persistencia.Json;
namespace SaoDimas.Infraestrutura.Repositories.Implementation;
internal sealed class ComunidadeJsonRepositorio(SessaoSistemaJson sessao) : IComunidadeRepositorio
{
    public async Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return sessao.Comunidades.FirstOrDefault(c => c.Id == id);
    }
    public async Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return sessao.Comunidades.OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Id).ToList();
    }
}
