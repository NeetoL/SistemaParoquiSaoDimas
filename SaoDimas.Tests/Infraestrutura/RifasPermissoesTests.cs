using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SaoDimas.CrossCutting.Filters;
namespace SaoDimas.Tests.Infraestrutura;
public sealed class RifasPermissoesTests {
 [Theory][InlineData("Administrador",true)][InlineData("Secretaria",true)][InlineData("Tesouraria",true)][InlineData("Coordenador",false)]
 public async Task Rifas_exigem_perfil_autorizado(string perfil,bool permitido){
  var http=new DefaultHttpContext {User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,perfil)},"teste"))};
  var rota=new RouteData();rota.Values["controller"]="Rifas";rota.Values["action"]="Reservar";
  var contexto=new AuthorizationFilterContext(new ActionContext(http,rota,new ActionDescriptor()),[]);
  await new PermissoesParoquiaisFilter().OnAuthorizationAsync(contexto);
  if(permitido)Assert.Null(contexto.Result);else Assert.Equal(403,Assert.IsType<StatusCodeResult>(contexto.Result).StatusCode);
 }
}
