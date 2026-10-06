using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;
public interface IGestaoParoquialAplicacao
{
 Task<IReadOnlyList<RegistroParoquial>> ListarAsync(string modulo,string? busca,DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct);
 Task<ResultadoGestao> SalvarAsync(string modulo,Guid? id,int revisao,Dictionary<string,string> campos,string operador,string? anexoNome,string? anexoBase64,CancellationToken ct);
 Task<ResultadoGestao> CancelarAsync(Guid id,int revisao,string operador,CancellationToken ct);
 Task<ResumoFinanceiro> ResumoAsync(DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct);
 Task<string> ExportarAsync(string modulo,DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct);
 Task<ResultadoGestao> ImportarAsync(string modulo,string csv,string operador,bool confirmar,CancellationToken ct);
}
