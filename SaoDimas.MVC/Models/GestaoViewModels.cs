using System.Globalization;
using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.MVC.Models;
public sealed record GestaoIndexViewModel(ModuloParoquial Modulo,IReadOnlyList<RegistroParoquial> Registros,Dictionary<string,Dictionary<string,string>> Opcoes,ResumoFinanceiro? Resumo,string? Busca,DateOnly? De,DateOnly? Ate,string? Comunidade,int Pagina)
{
 public string Exibir(CampoParoquial campo,string valor)=>GestaoFormularioViewModel.Formatar(campo,valor,Opcoes);
}
public sealed class GestaoFormularioViewModel
{
 public Guid? Id{get;set;} public int Revisao{get;set;} public Dictionary<string,string> Campos{get;set;}=[];
 [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public ModuloParoquial Modulo{get;set;}=null!;public Dictionary<string,Dictionary<string,string>> Opcoes{get;set;}=[];
 public string? AnexoNome{get;set;}
 public static string Formatar(CampoParoquial campo,string valor,Dictionary<string,Dictionary<string,string>> opcoes){
  if(opcoes.TryGetValue(campo.Tipo,out var itens))return itens.GetValueOrDefault(valor,valor);
  if(campo.Tipo=="money"&&decimal.TryParse(valor,CultureInfo.InvariantCulture,out var moeda))return moeda.ToString("C",CultureInfo.GetCultureInfo("pt-BR"));
  if(campo.Tipo is "date" or "datetime-local"&&DateTime.TryParse(valor,CultureInfo.InvariantCulture,out var data))return data.ToString(campo.Tipo=="date"?"dd/MM/yyyy":"dd/MM/yyyy HH:mm",CultureInfo.GetCultureInfo("pt-BR"));
  return valor;
 }
}
public sealed record GestaoDetalhesViewModel(RegistroParoquial Registro,ModuloParoquial Modulo,Dictionary<string,Dictionary<string,string>> Opcoes){
 public string Exibir(CampoParoquial campo)=>GestaoFormularioViewModel.Formatar(campo,Registro.Campos.GetValueOrDefault(campo.Chave,""),Opcoes);
}
public sealed record ImportacaoGestaoViewModel(string Modulo,string? Csv=null,string? Mensagem=null,bool Validada=false);
public sealed record UsuariosViewModel(IReadOnlyList<UsuarioParoquial> Usuarios,UsuarioParoquial? Editando=null);
public sealed record RelatoriosViewModel(ResumoFinanceiro? Resumo,IReadOnlyList<RegistroParoquial> Registros,DateOnly? De,DateOnly? Ate,string? Comunidade);
