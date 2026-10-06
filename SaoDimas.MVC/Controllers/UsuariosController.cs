using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
public sealed class UsuariosController(IUsuariosAplicacao usuarios):Controller
{
 public async Task<IActionResult> Index(Guid? id,CancellationToken ct){var lista=await usuarios.ListarAsync(ct);return View(new UsuariosViewModel(lista,lista.FirstOrDefault(u=>u.Id==id)));}
 [HttpPost]public async Task<IActionResult> Salvar(Guid? id,string login,string nome,string perfil,bool ativo,string? senha,CancellationToken ct){
  var resultado=await usuarios.SalvarAsync(id,login??"",nome??"",perfil??"",ativo,senha,User.Identity!.Name!,ct);
  if(resultado.Sucesso){TempData[MensagemTempData.Sucesso]=resultado.Mensagem;return RedirectToAction(nameof(Index));}
  ModelState.AddModelError("",resultado.Mensagem);var lista=await usuarios.ListarAsync(ct);return View("Index",new UsuariosViewModel(lista,lista.FirstOrDefault(u=>u.Id==id)));
 }
}
