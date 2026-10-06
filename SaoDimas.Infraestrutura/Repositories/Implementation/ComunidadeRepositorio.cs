using Microsoft.EntityFrameworkCore;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura.Persistencia;

namespace SaoDimas.Infraestrutura.Repositories.Implementation;

internal sealed class ComunidadeRepositorio(SaoDimasDbContext contexto) : IComunidadeRepositorio
{
    public Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Comunidades.AsNoTracking().FirstOrDefaultAsync(comunidade => comunidade.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken cancellationToken) =>
        await contexto.Comunidades
            .AsNoTracking()
            .OrderBy(comunidade => comunidade.OrdemExibicao)
            .ThenBy(comunidade => comunidade.Nome)
            .ToListAsync(cancellationToken);
}
