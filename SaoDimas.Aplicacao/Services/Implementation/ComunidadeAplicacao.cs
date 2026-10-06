using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class ComunidadeAplicacao(IComunidadeRepositorio comunidades) : IComunidadeAplicacao
{
    public async Task<IReadOnlyList<ComunidadeDto>> ListarTodasAsync(CancellationToken cancellationToken)
    {
        var todas = await comunidades.ListarAsync(cancellationToken);

        return todas.Select(ParaDto).ToList();
    }

    public async Task<IReadOnlyList<ComunidadeDto>> ListarParaVinculoAsync(int? comunidadeAtualId, CancellationToken cancellationToken)
    {
        var todas = await comunidades.ListarAsync(cancellationToken);

        return todas
            .Where(comunidade => comunidade.PodeReceberVinculos || comunidade.Id == comunidadeAtualId)
            .Select(ParaDto)
            .ToList();
    }

    private static ComunidadeDto ParaDto(Comunidade comunidade) =>
        new(comunidade.Id, comunidade.NomeCompleto, comunidade.Ativa);
}
