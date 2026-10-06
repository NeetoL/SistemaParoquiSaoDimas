using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio;

namespace SaoDimas.Aplicacao.Services.Interface;

public interface IDizimistaAplicacao
{
    Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken);

    Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken);

    Task<DadosDizimista?> ObterParaEdicaoAsync(int id, CancellationToken cancellationToken);

    /// <returns>O identificador do dizimista cadastrado.</returns>
    Task<Resultado<int>> CadastrarAsync(DadosDizimista dados, CancellationToken cancellationToken);

    Task<Resultado> AtualizarAsync(int id, DadosDizimista dados, CancellationToken cancellationToken);
}
