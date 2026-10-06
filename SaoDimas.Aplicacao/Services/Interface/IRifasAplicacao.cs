using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;

public interface IRifasAplicacao
{
    Task<IReadOnlyList<RifaDto>> ListarAsync(CancellationToken ct);
    Task<RifaDto?> ObterAsync(Guid id, CancellationToken ct);
    Task<ResultadoGestao> CriarAsync(DadosRifa dados, string operador, CancellationToken ct);
    Task<ResultadoGestao> ReservarAsync(Guid id, int revisao, string numeros, string comprador, string telefone, string vendedor, string operador, CancellationToken ct);
    Task<ResultadoGestao> PagamentoAsync(Guid id, int revisao, int numero, bool pago, string forma, string operador, CancellationToken ct);
    Task<ResultadoGestao> LiberarAsync(Guid id, int revisao, int numero, string operador, CancellationToken ct);
    Task<ResultadoGestao> SituacaoAsync(Guid id, int revisao, string situacao, string operador, CancellationToken ct);
    Task<ResultadoGestao> ResultadoAsync(Guid id, int revisao, int premio, int numero, string referencia, string operador, CancellationToken ct);
}
