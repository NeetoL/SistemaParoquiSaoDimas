using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;
public interface IGestaoParoquialDados
{
 Task<IReadOnlyList<RegistroParoquial>> RegistrosAsync(CancellationToken ct);
 Task<IReadOnlyList<UsuarioParoquial>> UsuariosAsync(CancellationToken ct);
 Task<IReadOnlyList<AuditoriaParoquial>> AuditoriaAsync(CancellationToken ct);
 Task GravarLoteAsync(IReadOnlyList<RegistroParoquial> registros,string operador,CancellationToken ct);
 Task GravarRegistroAsync(RegistroParoquial registro,string operador,string acao,CancellationToken ct);
 Task GravarUsuarioAsync(UsuarioParoquial usuario,string operador,CancellationToken ct);
 Task AuditarAsync(string operador,string acao,string modulo,string referencia,CancellationToken ct);
 Task<IReadOnlyList<BackupParoquial>> BackupsAsync(CancellationToken ct);
 Task<byte[]> CriarBackupAsync(CancellationToken ct);
 Task<string> ReceberBackupAsync(byte[] bytes,CancellationToken ct);
 Task<byte[]> LerBackupAsync(string nome,CancellationToken ct);
 Task RestaurarBackupAsync(string nome,string operador,CancellationToken ct);
}
