using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio;

namespace SaoDimas.Aplicacao.Services.Interface;

public interface IEventoAplicacao
{
    Task<PainelEventosDto> ObterPainelAsync(FiltroEventos filtro, CancellationToken cancellationToken);

    Task<EventoDetalhesDto?> ObterDetalhesAsync(int eventoId, CancellationToken cancellationToken);

    Task<DadosEvento?> ObterParaEdicaoAsync(int eventoId, CancellationToken cancellationToken);

    Task<Resultado<int>> CriarAsync(DadosEvento dados, CancellationToken cancellationToken);

    Task<Resultado> AtualizarAsync(int eventoId, DadosEvento dados, CancellationToken cancellationToken);

    Task<Resultado> AlterarStatusAsync(int eventoId, AcaoStatusEvento acao, CancellationToken cancellationToken, string? operador = null);

    Task<DadosProduto?> ObterProdutoAsync(int eventoId, int produtoId, CancellationToken cancellationToken);

    Task<Resultado<int>> AdicionarProdutoAsync(int eventoId, DadosProduto dados, CancellationToken cancellationToken);

    Task<Resultado> AtualizarProdutoAsync(int eventoId, int produtoId, DadosProduto dados, CancellationToken cancellationToken);
}
