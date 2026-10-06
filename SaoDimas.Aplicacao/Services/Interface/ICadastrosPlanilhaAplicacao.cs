using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;
public interface ICadastrosPlanilhaAplicacao { Task<ResultadoGestao> ImportarAsync(string csv,bool confirmar,string operador,CancellationToken ct);Task<string> ExportarAsync(CancellationToken ct); }
