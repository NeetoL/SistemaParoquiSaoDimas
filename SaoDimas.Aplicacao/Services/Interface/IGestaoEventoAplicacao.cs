using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;

namespace SaoDimas.Aplicacao.Services.Interface;

public interface IGestaoEventoAplicacao
{
    Task<GestaoEventoDto?> ObterAsync(int id, CancellationToken ct);
    Task<Resultado<int>> NovaEdicaoAsync(int id, int ano, DateOnly inicio, DateOnly? fim, bool produtos, bool precos, CancellationToken ct, string? operador = null);
    Task<Resultado> AbrirAsync(int id, decimal troco, string operador, string? observacao, CancellationToken ct);
    Task<Resultado> SituacaoAsync(int id, int loteId, int faixaId, int vendidos, int devolvidos, string operador, CancellationToken ct);
    Task<Resultado> CancelarTicketsAsync(int id, int loteId, int inicial, int final, string motivo, string operador, CancellationToken ct);
    Task<Resultado> ReceberAsync(int id, string responsavel, decimal valor, FormaRecebimento forma, string operador, string? observacao, CancellationToken ct, Guid? requisicaoId = null);
    Task<Resultado> EstornarAsync(int id, Guid lancamento, string motivo, string operador, CancellationToken ct);
    Task<Resultado> ConferirAsync(int id, decimal contado, string? justificativa, string operador, CancellationToken ct);
    Task<Resultado> FecharAsync(int id, bool excepcional, string? motivo, string? justificativa, string operador, CancellationToken ct);
}
