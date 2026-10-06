using System.Text;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
[Route("Gestao/{modulo}")]
public sealed class GestaoController(IGestaoParoquialAplicacao gestao,IGestaoParoquialDados dados,IComunidadeAplicacao comunidades,IDizimistaAplicacao dizimistas,IDocumentoParoquialService documentos):Controller
{
 private string Operador=>User.Identity!.Name!;
 [HttpGet("")]public async Task<IActionResult> Index(string modulo,string? busca,DateOnly? de,DateOnly? ate,string? comunidade,int pagina=1,CancellationToken ct=default){
  var catalogo=CatalogoParoquial.Obter(modulo);if(catalogo is null)return NotFound();
  if(de>ate){ModelState.AddModelError("","O início do período deve ser antes do fim.");}
  var opcoes=await OpcoesAsync(ct);
  var registros=await gestao.ListarAsync(modulo,null,de,ate,comunidade,ct);
  if(!string.IsNullOrWhiteSpace(busca))registros=registros.Where(r=>r.Campos.Values.Any(v=>v.Contains(busca,StringComparison.OrdinalIgnoreCase))||catalogo.Campos.Any(c=>GestaoFormularioViewModel.Formatar(c,r.Campos.GetValueOrDefault(c.Chave,""),opcoes).Contains(busca,StringComparison.OrdinalIgnoreCase))).ToArray();
  return View(new GestaoIndexViewModel(catalogo,registros,opcoes,modulo=="financeiro"?await gestao.ResumoAsync(de,ate,comunidade,ct):null,busca,de,ate,comunidade,Math.Clamp(pagina,1,Math.Max(1,(registros.Count+19)/20))));
 }
 [HttpGet("Novo")]public async Task<IActionResult> Novo(string modulo,CancellationToken ct){var catalogo=CatalogoParoquial.Obter(modulo);if(catalogo is null)return NotFound();return View("Formulario",new GestaoFormularioViewModel{Modulo=catalogo,Opcoes=await OpcoesAsync(ct)});}
 [HttpGet("{id:guid}/Editar")]public async Task<IActionResult> Editar(string modulo,Guid id,CancellationToken ct){var registro=(await dados.RegistrosAsync(ct)).FirstOrDefault(r=>r.Id==id&&r.Modulo==modulo&&!r.Cancelado);if(registro is null)return NotFound();if(modulo is "financeiro" or "contribuicoes")return RedirectToAction(nameof(Detalhes),new{modulo,id});return View("Formulario",new GestaoFormularioViewModel{Id=id,Revisao=registro.Revisao,Campos=registro.Campos,Modulo=CatalogoParoquial.Obter(modulo)!,Opcoes=await OpcoesAsync(ct),AnexoNome=registro.AnexoNome});}
 [HttpPost("Salvar"),RequestSizeLimit(8_000_000)]public async Task<IActionResult> Salvar(string modulo,GestaoFormularioViewModel formulario,IFormFile? comprovante,CancellationToken ct){
  string? arquivo=null,nome=null;if(comprovante is not null){if(comprovante.Length>5*1024*1024)ModelState.AddModelError("","Use um comprovante de até 5 MB.");else{using var memory=new MemoryStream();await comprovante.CopyToAsync(memory,ct);arquivo=Convert.ToBase64String(memory.ToArray());nome=Path.GetFileName(comprovante.FileName);}}
  ResultadoGestao? resultado=null;if(ModelState.IsValid)resultado=await gestao.SalvarAsync(modulo,formulario.Id,formulario.Revisao,formulario.Campos,Operador,nome,arquivo,ct);
  if(resultado?.Sucesso==true){TempData[MensagemTempData.Sucesso]=resultado.Mensagem;return RedirectToAction(nameof(Detalhes),new{modulo,id=resultado.Id});}
  if(resultado is not null)ModelState.AddModelError("",resultado.Mensagem);
  formulario.Modulo=CatalogoParoquial.Obter(modulo)!;formulario.Opcoes=await OpcoesAsync(ct);return View("Formulario",formulario);
 }
 [HttpGet("{id:guid}")]public async Task<IActionResult> Detalhes(string modulo,Guid id,CancellationToken ct){var registro=(await dados.RegistrosAsync(ct)).FirstOrDefault(r=>r.Id==id&&r.Modulo==modulo);return registro is null?NotFound():View(new GestaoDetalhesViewModel(registro,CatalogoParoquial.Obter(modulo)!,await OpcoesAsync(ct)));}
 [HttpPost("{id:guid}/Cancelar")]public async Task<IActionResult> Cancelar(string modulo,Guid id,int revisao,CancellationToken ct){if(!(await dados.RegistrosAsync(ct)).Any(r=>r.Id==id&&r.Modulo==modulo))return NotFound();var resultado=await gestao.CancelarAsync(id,revisao,Operador,ct);TempData[resultado.Sucesso?MensagemTempData.Sucesso:MensagemTempData.Erro]=resultado.Mensagem;return RedirectToAction(nameof(Index),new{modulo});}
 [HttpGet("{id:guid}/Documento")]public async Task<IActionResult> Documento(string modulo,Guid id,CancellationToken ct){var r=(await dados.RegistrosAsync(ct)).FirstOrDefault(r=>r.Id==id&&r.Modulo==modulo);if(r is null)return NotFound();var catalogo=CatalogoParoquial.Obter(modulo)!;var opcoes=await OpcoesAsync(ct);var campos=catalogo.Campos.ToDictionary(c=>c.Nome,c=>GestaoFormularioViewModel.Formatar(c,r.Campos.GetValueOrDefault(c.Chave,""),opcoes));campos["Registro"]=r.Id.ToString();campos["Situação"]=r.Cancelado?"CANCELADO":"Registrado";var titulo=modulo=="contribuicoes"?"Recibo de contribuição":modulo=="sacramentos"?"Certidão de registro de "+r.Campos["tipo"]:catalogo.Nome;return File(documentos.Gerar(titulo,[campos],Operador),"application/pdf","documento-"+id+".pdf");}
 [HttpGet("{id:guid}/Comprovante")]public async Task<IActionResult> Comprovante(string modulo,Guid id,CancellationToken ct){var r=(await dados.RegistrosAsync(ct)).FirstOrDefault(r=>r.Id==id&&r.Modulo==modulo);if(r?.AnexoBase64 is null)return NotFound();return File(Convert.FromBase64String(r.AnexoBase64),"application/octet-stream",r.AnexoNome??"comprovante");}
 [HttpGet("Exportar")]public async Task<IActionResult> Exportar(string modulo,DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct)=>File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(await gestao.ExportarAsync(modulo,de,ate,comunidade,ct))).ToArray(),"text/csv",modulo+".csv");
 [HttpGet("Importar")]public IActionResult Importar(string modulo)=>View(new ImportacaoGestaoViewModel(modulo));
 [HttpGet("Modelo")]public IActionResult Modelo(string modulo)=>File(Encoding.UTF8.GetBytes(string.Join(';',CatalogoParoquial.Obter(modulo)!.Campos.Select(c=>c.Chave))+"\n"),"text/csv","modelo-"+modulo+".csv");
 [HttpPost("Importar"),RequestSizeLimit(2_500_000)]public async Task<IActionResult> Importar(string modulo,IFormFile? planilha,string? csv,bool confirmar,CancellationToken ct){
  if(planilha is not null){using var leitor=new StreamReader(planilha.OpenReadStream(),Encoding.UTF8);csv=await leitor.ReadToEndAsync(ct);}
  var resultado=await gestao.ImportarAsync(modulo,csv??"",Operador,confirmar,ct);if(confirmar&&resultado.Sucesso){TempData[MensagemTempData.Sucesso]=resultado.Mensagem;return RedirectToAction(nameof(Index),new{modulo});}
  return View(new ImportacaoGestaoViewModel(modulo,csv,resultado.Mensagem,resultado.Sucesso&&!confirmar));
 }
 private async Task<Dictionary<string,Dictionary<string,string>>> OpcoesAsync(CancellationToken ct){
  var opcoes=new Dictionary<string,Dictionary<string,string>>{["comunidade"]=(await comunidades.ListarTodasAsync(ct)).ToDictionary(c=>c.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),c=>c.Nome),["dizimista"]=[]};
  var pagina=1;PaginaResultado<DizimistaResumoDto> pessoas;do{pessoas=await dizimistas.PesquisarAsync(new(){Pagina=pagina++,TamanhoPagina=100},ct);foreach(var d in pessoas.Itens)opcoes["dizimista"][d.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)]=d.Codigo+" · "+d.Nome;}while(pessoas.TemProxima);
  var registros=await dados.RegistrosAsync(ct);opcoes["pastoral"]=registros.Where(r=>r.Modulo=="pastorais"&&!r.Cancelado).ToDictionary(r=>r.Id.ToString(),r=>r.Campos["nome"]);opcoes["voluntario"]=registros.Where(r=>r.Modulo=="voluntarios"&&!r.Cancelado).ToDictionary(r=>r.Id.ToString(),r=>r.Campos["nome"]);return opcoes;
 }
}
