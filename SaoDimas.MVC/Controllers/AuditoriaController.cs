using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.MVC.Controllers;
public sealed class AuditoriaController(IGestaoParoquialDados dados):Controller
{
 public async Task<IActionResult> Index(string? busca,int pagina=1,CancellationToken ct=default){var lista=(await dados.AuditoriaAsync(ct)).Where(a=>string.IsNullOrWhiteSpace(busca)||(a.Operador+" "+a.Acao+" "+a.Modulo+" "+a.Referencia).Contains(busca,StringComparison.OrdinalIgnoreCase)).ToArray();ViewData["Busca"]=busca;ViewData["Total"]=lista.Length;ViewData["Pagina"]=Math.Max(1,pagina);return View(lista.Skip((Math.Max(1,pagina)-1)*50).Take(50).ToArray());}
}
