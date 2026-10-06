using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio;

namespace SaoDimas.Aplicacao.Services.Interface;

/// <summary>
/// Envelopes de Dízimo: os dados impressos são sempre consultados no backend a partir dos identificadores.
/// </summary>
public interface IEnvelopeDizimoAplicacao
{
    public const int LimitePorDocumento = 500;

    Task<Resultado<ArquivoPdf>> GerarAsync(int dizimistaId, CancellationToken cancellationToken);

    /// <summary>
    /// Um envelope por folha para cada dizimista selecionado (ordenados por nome).
    /// </summary>
    Task<Resultado<ArquivoPdf>> GerarEmLoteAsync(IReadOnlyCollection<int> dizimistaIds, CancellationToken cancellationToken);

    /// <summary>
    /// Um envelope por folha para cada dizimista ativo da comunidade (ordenados por nome).
    /// </summary>
    Task<Resultado<ArquivoPdf>> GerarParaComunidadeAsync(int comunidadeId, CancellationToken cancellationToken);
}
