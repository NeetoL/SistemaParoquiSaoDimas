using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Aplicacao.Queries.Interface;

/// <summary>
/// Consultas de leitura (projeções) de dizimistas. Implementadas na Infraestrutura.
/// </summary>
public interface IDizimistaConsultas
{
    Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken);

    Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Nome, código e comunidade dos dizimistas informados (os inexistentes são ignorados), ordenados por nome.
    /// </summary>
    Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Dados dos dizimistas ativos, ordenados por nome. Comunidade nula inclui todas.
    /// </summary>
    Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAtivosAsync(int? comunidadeId, CancellationToken cancellationToken);
}
