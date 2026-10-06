using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;

public interface IRifasDados
{
    Task<IReadOnlyList<RifaDto>> ListarAsync(CancellationToken ct);
    Task SalvarAsync(RifaDto rifa, string operador, string acao, CancellationToken ct);
}
