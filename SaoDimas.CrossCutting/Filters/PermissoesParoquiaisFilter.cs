using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.CrossCutting.Filters;
public sealed class PermissoesParoquiaisFilter:IAsyncAuthorizationFilter
{
 public Task OnAuthorizationAsync(AuthorizationFilterContext context){
  var user=context.HttpContext.User;if(user.Identity?.IsAuthenticated!=true)return Task.CompletedTask;
  var controller=context.RouteData.Values["controller"]?.ToString();var action=context.RouteData.Values["action"]?.ToString();
  var perfil=user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value??"";
  bool permitido=controller switch{
   "Rifas"=>perfil is "Administrador" or "Secretaria" or "Tesouraria",
   "Gestao"=>CatalogoParoquial.Permite(perfil,context.RouteData.Values["modulo"]?.ToString()??""),
   "Usuarios" or "Backups" or "Auditoria"=>perfil=="Administrador",
   "Relatorios"=>true,
   "Dizimistas" or "Envelopes" or "Cadastros"=>perfil is "Administrador" or "Secretaria" or "Tesouraria",
   "GestaoEventos"=>perfil is "Administrador" or "Tesouraria"||action is "Conteudo" or "Index",
   _=>true
  };
  if(!permitido)context.Result=new StatusCodeResult(403);return Task.CompletedTask;
 }
}
