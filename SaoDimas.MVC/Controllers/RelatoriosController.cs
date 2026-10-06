using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
public sealed class RelatoriosController(IGestaoParoquialAplicacao gestao,IDocumentoParoquialService documentos,IComunidadeAplicacao comunidades,IDizimistaAplicacao dizimistas,IGestaoParoquialDados dados):Controller
{
 public async Task<IActionResult> Index(string? modulo=null,DateOnly? de=null,DateOnly? ate=null,string? comunidade=null,bool pdf=false,bool csv=false,CancellationToken ct=default){
  modulo ??= CatalogoParoquial.Modulos.First(m=>m.Perfis.Contains(User.FindFirstValue(ClaimTypes.Role)!)).Chave;
  if(!CatalogoParoquial.Permite(User.FindFirstValue(ClaimTypes.Role)!,modulo))return StatusCode(403);
  var opcoes=new Dictionary<string,Dictionary<string,string>> { ["comunidade"]=(await comunidades.ListarTodasAsync(ct)).ToDictionary(c=>c.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),c=>c.Nome),["dizimista"]=[] };
  var pagina=1;PaginaResultado<DizimistaResumoDto> pessoas;do{pessoas=await dizimistas.PesquisarAsync(new(){Pagina=pagina++,TamanhoPagina=100},ct);foreach(var d in pessoas.Itens)opcoes["dizimista"][d.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)]=d.Nome;}while(pessoas.TemProxima);
  var todos=await dados.RegistrosAsync(ct);opcoes["pastoral"]=todos.Where(r=>r.Modulo=="pastorais").ToDictionary(r=>r.Id.ToString(),r=>r.Campos["nome"]);opcoes["voluntario"]=todos.Where(r=>r.Modulo=="voluntarios").ToDictionary(r=>r.Id.ToString(),r=>r.Campos["nome"]);
  ViewData["Comunidades"]=opcoes["comunidade"];
  if(de>ate)ModelState.AddModelError("","O início do período deve ser antes do fim.");
  var registros=await gestao.ListarAsync(modulo,null,de,ate,comunidade,ct);
  var saldo=modulo=="financeiro"?await gestao.ResumoAsync(de,ate,comunidade,ct):null;
  ViewData["Modulo"]=modulo;
  if(csv){
   var catalogo=CatalogoParoquial.Obter(modulo)!;
   static string Escapar(string valor){if(valor.Length>0&&"=+-@\t\r".Contains(valor[0]))valor="'"+valor;return "\""+valor.Replace("\"","\"\"")+"\"";}
   var arquivo=new System.Text.StringBuilder();
   arquivo.AppendLine(string.Join(';',catalogo.Campos.Select(c=>Escapar(c.Nome)).Append(Escapar("Origem"))));
   foreach(var registro in registros)arquivo.AppendLine(string.Join(';',catalogo.Campos.Select(c=>Escapar(GestaoFormularioViewModel.Formatar(c,registro.Campos.GetValueOrDefault(c.Chave,""),opcoes))).Append(Escapar(registro.Modulo=="contribuicoes"?"Contribuição":registro.Modulo=="eventos-caixa"?"Evento":"Cadastro"))));
   return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(arquivo.ToString())).ToArray(),"text/csv","relatorio-"+modulo+".csv");
  }
  if(pdf){var catalogo=CatalogoParoquial.Obter(modulo)!;var linhas=registros.Select(r=>catalogo.Campos.ToDictionary(c=>c.Nome,c=>GestaoFormularioViewModel.Formatar(c,r.Campos.GetValueOrDefault(c.Chave,""),opcoes))).ToList();if(saldo is not null)linhas.Insert(0,new(){["Entradas"]=saldo.Entradas.ToString("C",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),["Saídas"]=saldo.Saidas.ToString("C",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),["Saldo"]=saldo.Saldo.ToString("C",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))});return File(documentos.Gerar("Relatório · "+catalogo.Nome,linhas,User.Identity!.Name!),"application/pdf","relatorio-"+modulo+".pdf");}
  return View(new RelatoriosViewModel(saldo,registros,de,ate,comunidade));
 }
}
