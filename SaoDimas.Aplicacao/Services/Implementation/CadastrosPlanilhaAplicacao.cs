using System.Globalization;
using System.Text;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Aplicacao.Utilitarios;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.Repositories.Interface;
namespace SaoDimas.Aplicacao.Services.Implementation;
internal sealed class CadastrosPlanilhaAplicacao(IDizimistaRepositorio repositorio,IComunidadeRepositorio comunidades,IDizimistaAplicacao consultas,IGestaoParoquialDados auditoria,TimeProvider relogio):ICadastrosPlanilhaAplicacao
{
 private static readonly string[] Colunas=["nome","cpf","telefone","comunidadeId","dataEntrada","status","endereco","cep","bairro","dataNascimento"];
 public async Task<ResultadoGestao> ImportarAsync(string csv,bool confirmar,string operador,CancellationToken ct){
  if(csv.Length>2_000_000)return new(false,"Use um arquivo de até 2 MB.");List<string[]> linhas;try{linhas=CsvParoquial.Ler(csv);}catch(FormatException){return new(false,"CSV inválido.");}
  if(linhas.Count<2||linhas.Count>501)return new(false,"Importe de 1 a 500 cadastros.");var nomes=linhas[0].Select(s=>s.Trim().Trim('\uFEFF')).ToArray();if(nomes.Distinct().Count()!=nomes.Length||nomes.Any(n=>!Colunas.Contains(n)))return new(false,"Use as colunas do modelo.");
  var novos=new List<Dizimista>();int n=1;
  foreach(var linha in linhas.Skip(1)){n++;if(linha.Length!=nomes.Length)return new(false,"Colunas inválidas na linha "+n+".");var campos=nomes.Zip(linha).ToDictionary(p=>p.First,p=>p.Second);string V(string k)=>campos.GetValueOrDefault(k,"");
   if(!int.TryParse(V("comunidadeId"),out var comunidadeId)||!DateOnly.TryParseExact(V("dataEntrada"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var entrada)||!Enum.TryParse<StatusDizimista>(V("status"),out var status))return new(false,"Confira comunidade, dataEntrada (AAAA-MM-DD) e status (Ativo/Inativo) na linha "+n+".");
   DateOnly? nascimento=null;if(V("dataNascimento").Length>0){if(!DateOnly.TryParseExact(V("dataNascimento"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var data))return new(false,"Nascimento inválido na linha "+n+".");nascimento=data;}
   var comunidade=await comunidades.ObterPorIdAsync(comunidadeId,ct);if(comunidade is null)return new(false,"Comunidade não encontrada na linha "+n+".");
   var resultado=Dizimista.Criar(V("nome"),V("cpf"),V("telefone"),comunidade,entrada,status,relogio.GetLocalNow(),V("endereco"),V("cep"),V("bairro"),nascimento);
   if(!resultado.Sucesso)return new(false,"Linha "+n+": "+string.Join(" ",resultado.Erros.Select(e=>e.Mensagem)));
   var d=resultado.Valor;if(d.Cpf is not null&&(novos.Any(x=>x.Cpf?.Valor==d.Cpf.Valor)||await repositorio.ExisteCpfAsync(d.Cpf,null,ct)))return new(false,"CPF duplicado na linha "+n+".");novos.Add(d);
  }
  if(confirmar){foreach(var d in novos)repositorio.Adicionar(d);await repositorio.SalvarAlteracoesAsync(ct);await auditoria.AuditarAsync(operador,"Importação de dizimistas","dizimistas",novos.Count+" cadastros",ct);}
  return new(true,novos.Count+(confirmar?" dizimistas importados.":" cadastros validados. Nenhum dado foi alterado."));
 }
 public async Task<string> ExportarAsync(CancellationToken ct){
  var csv=new StringBuilder(string.Join(';',Colunas)+"\n");int pagina=1;PaginaResultado<DizimistaResumoDto> lista;
  do{lista=await consultas.PesquisarAsync(new(){Pagina=pagina++,TamanhoPagina=100},ct);foreach(var r in lista.Itens){var d=(await consultas.ObterParaEdicaoAsync(r.Id,ct))!;string Q(string? texto){texto??="";if(texto.Length>0&&"=+-@".Contains(texto[0]))texto="'"+texto;return "\""+texto.Replace("\"","\"\"")+"\"";}csv.AppendLine(string.Join(';',new[]{d.Nome,d.Cpf,d.Telefone,d.ComunidadeId.ToString(CultureInfo.InvariantCulture),d.DataEntrada.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),d.Status.ToString(),d.Endereco,d.Cep,d.Bairro,d.DataNascimento?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}.Select(Q)));}}while(lista.TemProxima);return csv.ToString();
 }
}
