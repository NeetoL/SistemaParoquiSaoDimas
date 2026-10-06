using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio;

namespace SaoDimas.Aplicacao.Services.Interface;

/// <summary>
/// Lotes de tickets, distribuição em faixas, prestação de contas e impressão.
/// Todo lote é acessado pelo seu evento (o lote precisa pertencer ao evento informado).
/// </summary>
public interface ITicketAplicacao
{
    Task<PreparacaoLoteDto?> ObterPreparacaoLoteAsync(int eventoId, int produtoId, CancellationToken cancellationToken);

    Task<SimulacaoLoteDto> SimularLoteAsync(int eventoId, int produtoId, int quantidade, CancellationToken cancellationToken);

    /// <returns>Id do lote gerado.</returns>
    Task<Resultado<int>> GerarLoteAsync(int eventoId, int produtoId, int quantidade, CancellationToken cancellationToken);

    Task<LoteDetalhesDto?> ObterLoteAsync(int eventoId, int loteId, CancellationToken cancellationToken);

    Task<DadosDistribuicao?> ObterDistribuicaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken);

    Task<Resultado> DistribuirAsync(int eventoId, int loteId, DadosDistribuicao dados, CancellationToken cancellationToken);

    Task<Resultado> AlterarDistribuicaoAsync(int eventoId, int loteId, int distribuicaoId, DadosDistribuicao dados, CancellationToken cancellationToken);

    Task<Resultado> RemoverDistribuicaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken, string? operador = null);

    Task<PreparacaoPrestacaoDto?> ObterPrestacaoAsync(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken);

    Task<SimulacaoPrestacaoDto?> SimularPrestacaoAsync(
        int eventoId, int loteId, int distribuicaoId, int vendidos, decimal valorEntregue, CancellationToken cancellationToken);

    Task<Resultado> RegistrarPrestacaoAsync(int eventoId, int loteId, int distribuicaoId, DadosPrestacao dados, CancellationToken cancellationToken);

    /// <summary>
    /// PDF A4 dos tickets do lote inteiro ou de uma única faixa (<paramref name="distribuicaoId"/>).
    /// </summary>
    Task<Resultado<ArquivoPdf>> GerarPdfAsync(int eventoId, int loteId, int? distribuicaoId, CancellationToken cancellationToken);
}
