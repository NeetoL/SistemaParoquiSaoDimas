using System.Text;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
public sealed class CadastrosController(ICadastrosPlanilhaAplicacao planilhas):Controller
{
 public async Task<IActionResult> Exportar(CancellationToken ct)=>File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(await planilhas.ExportarAsync(ct))).ToArray(),"text/csv","dizimistas.csv");
 public IActionResult Modelo()=>File(Encoding.UTF8.GetBytes("nome;cpf;telefone;comunidadeId;dataEntrada;status;endereco;cep;bairro;dataNascimento\n"),"text/csv","modelo-dizimistas.csv");
 public IActionResult Importar()=>View(new ImportacaoGestaoViewModel("dizimistas"));
 [HttpPost,RequestSizeLimit(2_500_000)]public async Task<IActionResult> Importar(IFormFile? planilha,string? csv,bool confirmar,CancellationToken ct){if(planilha is not null){using var leitor=new StreamReader(planilha.OpenReadStream(),Encoding.UTF8);csv=await leitor.ReadToEndAsync(ct);}var r=await planilhas.ImportarAsync(csv??"",confirmar,User.Identity!.Name!,ct);if(confirmar&&r.Sucesso){TempData[MensagemTempData.Sucesso]=r.Mensagem;return RedirectToAction("Index","Dizimistas");}return View(new ImportacaoGestaoViewModel("dizimistas",csv,r.Mensagem,r.Sucesso&&!confirmar));}
}
