using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;
public interface IUsuariosAplicacao
{
 Task<IReadOnlyList<UsuarioParoquial>> ListarAsync(CancellationToken ct);
 Task<UsuarioParoquial?> AutenticarAsync(string login,string senha,CancellationToken ct);
 Task<ResultadoGestao> SalvarAsync(Guid? id,string login,string nome,string perfil,bool ativo,string? senha,string operador,CancellationToken ct);
 Task<ResultadoGestao> AlterarSenhaAsync(Guid id,string atual,string nova,CancellationToken ct);
}
