using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.Aplicacao.Services.Implementation;
internal sealed class UsuariosAplicacao(IGestaoParoquialDados dados,ICredenciaisService credenciais,IOptions<ConfiguracaoLogin> config):IUsuariosAplicacao
{
 public async Task<IReadOnlyList<UsuarioParoquial>> ListarAsync(CancellationToken ct)
 {
  var usuarios=await dados.UsuariosAsync(ct);
  if(usuarios.Count==0) { await dados.GravarUsuarioAsync(new(Guid.NewGuid(),config.Value.Usuario,"Administrador","Administrador",true,config.Value.SenhaHash),"sistema",ct);usuarios=await dados.UsuariosAsync(ct); }
  return usuarios;
 }
 public async Task<UsuarioParoquial?> AutenticarAsync(string login,string senha,CancellationToken ct)
 {
  var usuario=(await ListarAsync(ct)).FirstOrDefault(u=>u.Login.Equals(login.Trim(),StringComparison.OrdinalIgnoreCase));
  var valido=credenciais.Verificar(usuario?.SenhaHash??config.Value.SenhaHash,senha);
  if(usuario is null||!usuario.Ativo||!valido)return null;
  await dados.AuditarAsync(usuario.Login,"Acesso ao sistema","usuarios",usuario.Id.ToString(),ct);return usuario;
 }
 public async Task<ResultadoGestao> SalvarAsync(Guid? id,string login,string nome,string perfil,bool ativo,string? senha,string operador,CancellationToken ct)
 {
  var usuarios=await ListarAsync(ct);var atual=usuarios.FirstOrDefault(u=>u.Id==id);
  if(id is not null&&atual is null)return new(false,"Usuário não encontrado.");
  login=login.Trim();nome=nome.Trim();
  if(login.Length is <3 or >80||!login.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '_' or '-')||nome.Length is <2 or >120||!CatalogoParoquial.Perfis.Contains(perfil))return new(false,"Informe nome, login válido e perfil.");
  if(usuarios.Any(u=>u.Id!=id&&u.Login.Equals(login,StringComparison.OrdinalIgnoreCase)))return new(false,"Este login já está em uso.");
  if(atual is null&&string.IsNullOrWhiteSpace(senha))return new(false,"Informe a senha inicial.");
  if(!string.IsNullOrEmpty(senha)&&senha.Length is <8 or >128)return new(false,"A senha deve ter de 8 a 128 caracteres.");
  if(atual?.Perfil=="Administrador"&&atual.Ativo&&(!ativo||perfil!="Administrador")&&!usuarios.Any(u=>u.Id!=id&&u.Ativo&&u.Perfil=="Administrador"))return new(false,"Mantenha pelo menos um administrador ativo.");
  await dados.GravarUsuarioAsync(new(id??Guid.NewGuid(),login,nome,perfil,ativo,string.IsNullOrEmpty(senha)?atual!.SenhaHash:credenciais.GerarHash(senha),(atual?.Versao??0)+1),operador,ct);return new(true,"Usuário salvo.");
 }
 public async Task<ResultadoGestao> AlterarSenhaAsync(Guid id,string atual,string nova,CancellationToken ct)
 {
  var usuario=(await ListarAsync(ct)).FirstOrDefault(u=>u.Id==id);
  if(usuario is null||!credenciais.Verificar(usuario.SenhaHash,atual))return new(false,"A senha atual não confere.");
  if(nova.Length is <8 or >128)return new(false,"A nova senha deve ter de 8 a 128 caracteres.");
  await dados.GravarUsuarioAsync(usuario with{SenhaHash=credenciais.GerarHash(nova),Versao=usuario.Versao+1},usuario.Login,ct);return new(true,"Senha alterada. Entre novamente.");
 }
}
