using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.CrossCutting.Filters;
public sealed class AuditoriaCadastrosFilter(IGestaoParoquialDados dados):IAsyncActionFilter
{
 public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next){
  var resultado=await next();var controller=context.RouteData.Values["controller"]?.ToString();
  if(context.HttpContext.Request.Method=="POST"&&controller is "Dizimistas" or "Envelopes" or "Eventos" or "Tickets" or "GestaoEventos"&&context.ModelState.IsValid&&resultado.Exception is null){
   var acao=context.RouteData.Values["action"]?.ToString()??"";
   if(acao is "Painel" or "Conteudo" || (acao=="Editar"&&controller!="Dizimistas") || (acao=="Novo"&&controller!="Dizimistas"))return;
   if(resultado.Result is ObjectResult o&&o.StatusCode>=400||resultado.Result is ContentResult c&&c.StatusCode>=400)return;
   await dados.AuditarAsync(context.HttpContext.User.Identity?.Name??"sistema",acao,controller!,context.RouteData.Values["id"]?.ToString()??"",context.HttpContext.RequestAborted);
  }
 }
}
