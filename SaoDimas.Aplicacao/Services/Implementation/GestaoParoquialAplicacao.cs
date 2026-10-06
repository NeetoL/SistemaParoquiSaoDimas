using System.Globalization;
using System.Text;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.Aplicacao.Services.Implementation;
internal sealed class GestaoParoquialAplicacao(IGestaoParoquialDados dados,IDizimistaAplicacao dizimistas,IComunidadeAplicacao comunidades):IGestaoParoquialAplicacao
{
 private static readonly string[] ChavesTermo = ["tipo","livro","folha","termo","comunidade"];
 public async Task<IReadOnlyList<RegistroParoquial>> ListarAsync(string modulo,string? busca,DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct){
  var registros=(await dados.RegistrosAsync(ct)).Where(r=>!r.Cancelado&&(r.Modulo==modulo||(modulo=="financeiro"&&r.Modulo is "contribuicoes" or "eventos-caixa")));
  if(modulo=="financeiro"){
   var completos=new List<RegistroParoquial>();
   foreach(var r in registros){
    if(r.Modulo!="contribuicoes"){completos.Add(r);continue;}
    var campos=r.Campos.ToDictionary(c=>c.Key,c=>c.Value);
    var pessoa=int.TryParse(campos.GetValueOrDefault("dizimista"),out var pessoaId)?await dizimistas.ObterDetalhesAsync(pessoaId,ct):null;
    campos["descricao"]=campos.GetValueOrDefault("tipo","Contribuição")+" · "+(pessoa?.Nome??"Dizimista");
    campos["natureza"]="Entrada";campos["categoria"]="Contribuições";
    completos.Add(r with{Campos=campos});
   }
   registros=completos;
  }
  return registros.Where(r=>(string.IsNullOrWhiteSpace(busca)||r.Campos.Values.Any(v=>v.Contains(busca,StringComparison.OrdinalIgnoreCase)))&&(string.IsNullOrWhiteSpace(comunidade)||r.Campos.GetValueOrDefault("comunidade")==comunidade)&&Dentro(r,de,ate)).OrderByDescending(r=>r.Campos.GetValueOrDefault("data",r.Campos.GetValueOrDefault("inicio",""))).ThenByDescending(r=>r.CriadoEm).ToArray();
 }
 private static bool Dentro(RegistroParoquial r,DateOnly? de,DateOnly? ate){var valor=r.Campos.GetValueOrDefault("data",r.Campos.GetValueOrDefault("inicio",""));if(de is null&&ate is null)return true;if(!DateTime.TryParse(valor,CultureInfo.InvariantCulture,out var data))return false;var dia=DateOnly.FromDateTime(data);return(de is null||dia>=de)&&(ate is null||dia<=ate);}
 private static decimal Valor(RegistroParoquial r)=>decimal.Parse(r.Campos["valor"],CultureInfo.InvariantCulture);
 public async Task<ResumoFinanceiro> ResumoAsync(DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct){var registros=await ListarAsync("financeiro",null,de,ate,comunidade,ct);var entradas=registros.Where(r=>r.Modulo=="contribuicoes"||r.Campos.GetValueOrDefault("natureza")=="Entrada").Sum(Valor);var saidas=registros.Where(r=>r.Campos.GetValueOrDefault("natureza")=="Saída").Sum(Valor);return new(entradas,saidas,entradas-saidas);}
 public async Task<ResultadoGestao> SalvarAsync(string modulo,Guid? id,int revisao,Dictionary<string,string> campos,string operador,string? anexoNome,string? anexoBase64,CancellationToken ct){
  var lista=await dados.RegistrosAsync(ct);var atual=lista.FirstOrDefault(r=>r.Id==id);if(id is not null&&(atual is null||atual.Modulo!=modulo||atual.Cancelado))return new(false,"Registro não encontrado.");
  if(atual is not null&&atual.Revisao!=revisao)return new(false,"Este registro foi alterado por outra pessoa. Reabra a edição.");
  if(atual is not null&&modulo is "financeiro" or "contribuicoes")return new(false,"Para corrigir um lançamento, cancele-o e registre o correto.");
  var validacao=await ValidarAsync(modulo,id,campos,lista,ct);if(!validacao.Sucesso)return validacao;
  if(anexoBase64 is not null){byte[] bytes;try{bytes=Convert.FromBase64String(anexoBase64);}catch(FormatException){return new(false,"Comprovante inválido.");}if(bytes.Length>5*1024*1024||bytes.Length<4||!(bytes.AsSpan().StartsWith("%PDF"u8)||bytes.AsSpan().StartsWith(new byte[]{137,80,78,71})||bytes.AsSpan().StartsWith(new byte[]{255,216,255})))return new(false,"Use PDF, PNG ou JPEG de até 5 MB.");}
  var registro=new RegistroParoquial(id??Guid.NewGuid(),modulo,campos,atual?.CriadoEm??DateTime.UtcNow,atual?.CriadoPor??operador,(atual?.Revisao??0)+1,false,anexoNome??atual?.AnexoNome,anexoBase64??atual?.AnexoBase64);
  await dados.GravarRegistroAsync(registro,operador,atual is null?"Cadastro":"Edição",ct);return new(true,"Registro salvo.",registro.Id);
 }
 private async Task<ResultadoGestao> ValidarAsync(string modulo,Guid? id,Dictionary<string,string> campos,IReadOnlyList<RegistroParoquial> lista,CancellationToken ct){
  var catalogo=CatalogoParoquial.Obter(modulo);if(catalogo is null)return new(false,"Módulo inválido.");
  if(campos.Keys.Any(k=>!catalogo.Campos.Any(c=>c.Chave==k)))return new(false,"Campo desconhecido.");
  foreach(var campo in catalogo.Campos){
   var valor=(campos.GetValueOrDefault(campo.Chave,"")??"").Trim();campos[campo.Chave]=valor;
   if(campo.Obrigatorio&&valor.Length==0)return new(false,"Informe "+campo.Nome+".");if(valor.Length>2000)return new(false,campo.Nome+" excede o tamanho permitido.");if(valor.Length==0)continue;
   if(campo.Opcoes is not null&&!campo.Opcoes.Contains(valor))return new(false,"Opção inválida em "+campo.Nome+".");
   if(campo.Tipo=="money"){if(!decimal.TryParse(valor.Replace(",","."),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var dinheiro)||dinheiro<=0||dinheiro>100000000||decimal.Round(dinheiro,2)!=dinheiro)return new(false,"Informe um valor positivo com até duas casas decimais.");campos[campo.Chave]=dinheiro.ToString(CultureInfo.InvariantCulture);}
   if(campo.Tipo=="date"&&!DateOnly.TryParseExact(valor,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))return new(false,"Data inválida.");
   if(campo.Tipo=="month"&&!DateTime.TryParseExact(valor,"yyyy-MM",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))return new(false,"Competência inválida.");
   if(campo.Tipo=="datetime-local"&&!DateTime.TryParseExact(valor,"yyyy-MM-ddTHH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))return new(false,"Horário inválido.");
   if(campo.Tipo=="email"&&!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(valor))return new(false,"E-mail inválido.");
   if(campo.Tipo=="comunidade"&&!(await comunidades.ListarTodasAsync(ct)).Any(c=>c.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)==valor&&c.Ativa))return new(false,"Comunidade inválida.");
   if(campo.Tipo=="dizimista"&&(!int.TryParse(valor,out var pessoa)||await dizimistas.ObterDetalhesAsync(pessoa,ct) is null))return new(false,"Dizimista não encontrado.");
   if(campo.Tipo is "pastoral" or "voluntario" && !lista.Any(r=>!r.Cancelado&&r.Modulo==(campo.Tipo=="pastoral"?"pastorais":"voluntarios")&&r.Id.ToString()==valor))return new(false,campo.Nome+" não encontrado.");
  }
  if(modulo=="sacramentos"){if(campos["tipo"]=="Casamento"&&string.IsNullOrWhiteSpace(campos.GetValueOrDefault("conjuge")))return new(false,"Informe o cônjuge.");if(lista.Any(r=>r.Id!=id&&!r.Cancelado&&r.Modulo==modulo&&ChavesTermo.All(k=>r.Campos.GetValueOrDefault(k)==campos.GetValueOrDefault(k))))return new(false,"Este termo já foi registrado no livro informado.");}
  if(modulo is "agenda" or "escalas"){
   var inicio=DateTime.Parse(campos["inicio"],CultureInfo.InvariantCulture);var fim=DateTime.Parse(campos["fim"],CultureInfo.InvariantCulture);if(fim<=inicio)return new(false,"O término deve ser depois do início.");
   if(lista.Any(r=>r.Id!=id&&!r.Cancelado&&r.Modulo==modulo&&(modulo=="agenda"?r.Campos["local"].Equals(campos["local"],StringComparison.OrdinalIgnoreCase):r.Campos["voluntario"]==campos["voluntario"])&&DateTime.Parse(r.Campos["inicio"],CultureInfo.InvariantCulture)<fim&&DateTime.Parse(r.Campos["fim"],CultureInfo.InvariantCulture)>inicio))return new(false,modulo=="agenda"?"Este espaço já está reservado nesse horário.":"Este voluntário já está escalado nesse horário.");
  }
  return new(true,"Dados válidos.");
 }
 public async Task<ResultadoGestao> CancelarAsync(Guid id,int revisao,string operador,CancellationToken ct){var lista=await dados.RegistrosAsync(ct);var atual=lista.FirstOrDefault(r=>r.Id==id);if(atual is null||atual.Cancelado)return new(false,"Registro não encontrado.");if(atual.Revisao!=revisao)return new(false,"Registro alterado por outra pessoa.");if(atual.Modulo is "pastorais" or "voluntarios"&&lista.Any(r=>!r.Cancelado&&r.Campos.ContainsValue(id.ToString())))return new(false,"Há vínculos ativos. Cancele ou altere os vínculos primeiro.");await dados.GravarRegistroAsync(atual with{Cancelado=true,Revisao=atual.Revisao+1},operador,"Cancelamento",ct);return new(true,"Registro cancelado. O histórico foi preservado.");}
 public async Task<string> ExportarAsync(string modulo,DateOnly? de,DateOnly? ate,string? comunidade,CancellationToken ct){
  var catalogo=CatalogoParoquial.Obter(modulo)??throw new ArgumentException("Módulo inválido.");var linhas=new StringBuilder();linhas.AppendLine(string.Join(';',catalogo.Campos.Select(c=>c.Chave)));
  foreach(var r in (await ListarAsync(modulo,null,de,ate,comunidade,ct)).Where(r=>r.Modulo==modulo))linhas.AppendLine(string.Join(';',catalogo.Campos.Select(c=>Escapar(r.Campos.GetValueOrDefault(c.Chave,"")))));return linhas.ToString();
 }
 private static string Escapar(string texto){if(texto.Length>0&&"=+-@\t\r".Contains(texto[0]))texto="'"+texto;return "\""+texto.Replace("\"","\"\"")+"\"";}
 public async Task<ResultadoGestao> ImportarAsync(string modulo,string csv,string operador,bool confirmar,CancellationToken ct){
  var catalogo=CatalogoParoquial.Obter(modulo);if(catalogo is null)return new(false,"Módulo inválido.");if(csv.Length>2_000_000)return new(false,"A planilha excede 2 MB.");
  List<string[]> linhas;try{linhas=SaoDimas.Aplicacao.Utilitarios.CsvParoquial.Ler(csv);}catch(FormatException){return new(false,"CSV inválido.");}if(linhas.Count<2||linhas.Count>501)return new(false,"Importe de 1 a 500 registros por vez.");
  var cabecalho=linhas[0].Select(s=>s.Trim().Trim('\uFEFF')).ToArray();if(cabecalho.Distinct().Count()!=cabecalho.Length||cabecalho.Any(k=>!catalogo.Campos.Any(c=>c.Chave==k)))return new(false,"Use os cabeçalhos do modelo CSV.");
  var lista=(await dados.RegistrosAsync(ct)).ToList();var novos=new List<RegistroParoquial>();int n=1;
  foreach(var linha in linhas.Skip(1)){n++;if(linha.Length!=cabecalho.Length)return new(false,"Quantidade de colunas inválida na linha "+n+".");var campos=cabecalho.Zip(linha).ToDictionary(p=>p.First,p=>p.Second);var resultado=await ValidarAsync(modulo,null,campos,lista,ct);if(!resultado.Sucesso)return new(false,"Linha "+n+": "+resultado.Mensagem);if(lista.Any(r=>r.Modulo==modulo&&!r.Cancelado&&r.Campos.Count==campos.Count&&campos.All(c=>r.Campos.GetValueOrDefault(c.Key)==c.Value)))return new(false,"Registro duplicado na linha "+n+".");var novo=new RegistroParoquial(Guid.NewGuid(),modulo,campos,DateTime.UtcNow,operador);novos.Add(novo);lista.Add(novo);}
  if(confirmar)await dados.GravarLoteAsync(novos,operador,ct);
  return new(true,confirmar?novos.Count+" registros importados.":novos.Count+" registros validados. Nenhum dado foi alterado.");
 }

}