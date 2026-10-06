using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
public sealed class BackupsController(IGestaoParoquialDados dados):Controller
{
 public async Task<IActionResult> Index(CancellationToken ct)=>View(new BackupsViewModel(await dados.BackupsAsync(ct)));
 [HttpPost]public async Task<IActionResult> Criar(CancellationToken ct){var bytes=await dados.CriarBackupAsync(ct);await dados.AuditarAsync(User.Identity!.Name!,"Criação de backup","backups","manual",ct);return File(bytes,"application/json","backup-paroquia.json");}
 [HttpPost,RequestSizeLimit(110_000_000)]public async Task<IActionResult> Enviar(IFormFile? backup,CancellationToken ct){if(backup is null||backup.Length>100*1024*1024)return BadRequest("Selecione um backup de até 100 MB.");using var memory=new MemoryStream();await backup.CopyToAsync(memory,ct);try{var nome=await dados.ReceberBackupAsync(memory.ToArray(),ct);return RedirectToAction(nameof(Restaurar),new{nome});}catch(InvalidDataException e){return BadRequest(e.Message);}}
 public async Task<IActionResult> Baixar(string nome,CancellationToken ct){try{return File(await dados.LerBackupAsync(nome,ct),"application/json",nome);}catch(InvalidDataException){return BadRequest();}catch(FileNotFoundException){return NotFound();}}
 [HttpGet]public async Task<IActionResult> Restaurar(string nome,CancellationToken ct){try{var bytes=await dados.LerBackupAsync(nome,ct);using var doc=JsonDocument.Parse(bytes);var json=doc.RootElement;return View(new RestaurarBackupViewModel(nome,Convert.ToHexString(SHA256.HashData(bytes)),json.GetProperty("dizimistas").GetArrayLength(),json.TryGetProperty("gestao",out var gestao)?gestao.GetArrayLength():0,json.GetProperty("eventos").GetProperty("eventos").GetArrayLength(),json.TryGetProperty("rifas",out var rifas)?rifas.GetArrayLength():0));}catch(InvalidDataException){return BadRequest();}catch(FileNotFoundException){return NotFound();}}
 [HttpPost]public async Task<IActionResult> Restaurar(string nome,string hash,string confirmacao,CancellationToken ct){
  try{var bytes=await dados.LerBackupAsync(nome,ct);if(confirmacao!="RESTAURAR"||Convert.ToHexString(SHA256.HashData(bytes))!=hash)return BadRequest("Confira o backup e digite RESTAURAR antes de continuar.");
  await dados.RestaurarBackupAsync(nome,User.Identity!.Name!,ct);TempData[MensagemTempData.Sucesso]="Backup restaurado. Uma cópia do estado anterior foi preservada.";return RedirectToAction(nameof(Index));}
  catch(InvalidDataException e){return BadRequest(e.Message);}catch(FileNotFoundException){return NotFound();}
 }
}
