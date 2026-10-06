using System.Text.Json;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Infraestrutura.Persistencia.Json;
namespace SaoDimas.Infraestrutura.Services.Implementation;
internal sealed class GestaoParoquialDados(SessaoSistemaJson sessao,ArquivoSistemaJson arquivo):IGestaoParoquialDados
{
 private string Pasta=>Path.Combine(Path.GetDirectoryName(arquivo.Caminho)!,"backups");
 public async Task<IReadOnlyList<RegistroParoquial>> RegistrosAsync(CancellationToken ct){
  await sessao.CarregarAsync(ct);var lista=sessao.Documento.Gestao.ToList();
  foreach(var evento in sessao.Documento.Eventos.Eventos)foreach(var mov in evento.Caixa?.Movimentacoes??[])
   lista.Add(new(mov.Id,"eventos-caixa",new(){["descricao"]=evento.Nome+" · "+mov.Responsavel,["data"]=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(mov.Data,"America/Sao_Paulo").DateTime).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),["natureza"]=mov.Valor<0?"Saída":"Entrada",["valor"]=Math.Abs(mov.Valor).ToString(System.Globalization.CultureInfo.InvariantCulture),["forma"]=mov.Forma.ToString(),["categoria"]="Eventos",["comunidade"]=evento.ComunidadeId.ToString(System.Globalization.CultureInfo.InvariantCulture),["origem"]="Caixa do evento",["eventoId"]=evento.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),["observacao"]=mov.Observacao??""},mov.Data.UtcDateTime,mov.Operador));
  return lista;
 }
 public async Task<IReadOnlyList<UsuarioParoquial>> UsuariosAsync(CancellationToken ct){await sessao.CarregarAsync(ct);return sessao.Documento.Usuarios.ToArray();}
 public async Task<IReadOnlyList<AuditoriaParoquial>> AuditoriaAsync(CancellationToken ct){await sessao.CarregarAsync(ct);return sessao.Documento.Auditoria.OrderByDescending(a=>a.Data).ToArray();}
 private void Registrar(string operador,string acao,string modulo,string referencia,Dictionary<string,string>? antes=null,Dictionary<string,string>? depois=null)=>sessao.Documento.Auditoria.Add(new(Guid.NewGuid(),DateTime.UtcNow,operador,acao,modulo,referencia,antes,depois));
 public async Task GravarLoteAsync(IReadOnlyList<RegistroParoquial> registros,string operador,CancellationToken ct){await sessao.CarregarAsync(ct);sessao.Documento.Gestao.AddRange(registros);Registrar(operador,"Importação CSV","importacao",registros.Count+" registros");await sessao.SalvarAsync(ct);}
 public async Task GravarRegistroAsync(RegistroParoquial registro,string operador,string acao,CancellationToken ct){
  await sessao.CarregarAsync(ct);var antes=sessao.Documento.Gestao.FirstOrDefault(r=>r.Id==registro.Id)?.Campos.ToDictionary(c=>c.Key,c=>c.Value);sessao.Documento.Gestao.RemoveAll(r=>r.Id==registro.Id);sessao.Documento.Gestao.Add(registro);Registrar(operador,acao,registro.Modulo,registro.Id.ToString(),antes,registro.Campos.ToDictionary(c=>c.Key,c=>c.Value));await sessao.SalvarAsync(ct);
 }
 public async Task GravarUsuarioAsync(UsuarioParoquial usuario,string operador,CancellationToken ct){
  await sessao.CarregarAsync(ct);sessao.Documento.Usuarios.RemoveAll(r=>r.Id==usuario.Id);sessao.Documento.Usuarios.Add(usuario);Registrar(operador,"Atualização de usuário","usuarios",usuario.Login);await sessao.SalvarAsync(ct);
 }
 public async Task AuditarAsync(string operador,string acao,string modulo,string referencia,CancellationToken ct){await sessao.CarregarAsync(ct);Registrar(operador,acao,modulo,referencia);await sessao.SalvarAsync(ct);}
 public async Task<IReadOnlyList<BackupParoquial>> BackupsAsync(CancellationToken ct){await sessao.CarregarAsync(ct);Directory.CreateDirectory(Pasta);return Directory.GetFiles(Pasta,"*.json").Select(p=>new FileInfo(p)).OrderByDescending(f=>f.LastWriteTimeUtc).Select(f=>new BackupParoquial(f.Name,f.LastWriteTimeUtc,f.Length)).ToArray();}
 private string CaminhoSeguro(string nome){if(nome!=Path.GetFileName(nome)||!System.Text.RegularExpressions.Regex.IsMatch(nome,@"^(automatico|manual|antes-restauracao)-[0-9A-Za-z-]+\.json$"))throw new InvalidDataException("Nome de backup inválido.");return Path.Combine(Pasta,nome);}
 public async Task<byte[]> CriarBackupAsync(CancellationToken ct){await sessao.CarregarAsync(ct);await sessao.SalvarAsync(ct);var bytes=await File.ReadAllBytesAsync(arquivo.Caminho,ct);Directory.CreateDirectory(Pasta);await File.WriteAllBytesAsync(Path.Combine(Pasta,"manual-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+".json"),bytes,ct);return bytes;}
 public async Task<string> ReceberBackupAsync(byte[] bytes,CancellationToken ct){await sessao.CarregarAsync(ct);if(bytes.Length>100*1024*1024)throw new InvalidDataException("Use um backup de até 100 MB.");DocumentoSistema? documento;try{using var json=JsonDocument.Parse(bytes);if(!json.RootElement.TryGetProperty("versao",out _)||!json.RootElement.TryGetProperty("comunidades",out _)||!json.RootElement.TryGetProperty("dizimistas",out _)||!json.RootElement.TryGetProperty("eventos",out _))throw new InvalidDataException("Backup incompleto.");documento=JsonSerializer.Deserialize<DocumentoSistema>(bytes,ArquivoSistemaJson.Serializacao);}catch(JsonException){throw new InvalidDataException("Arquivo de backup inválido.");}ArquivoSistemaJson.Validar(documento);Directory.CreateDirectory(Pasta);var nome="manual-"+Guid.NewGuid().ToString("N")+".json";await File.WriteAllBytesAsync(Path.Combine(Pasta,nome),bytes,ct);return nome;}
 public async Task<byte[]> LerBackupAsync(string nome,CancellationToken ct){await sessao.CarregarAsync(ct);return await File.ReadAllBytesAsync(CaminhoSeguro(nome),ct);}
 public async Task RestaurarBackupAsync(string nome,string operador,CancellationToken ct){
  var bytes=await LerBackupAsync(nome,ct);var restaurado=JsonSerializer.Deserialize<DocumentoSistema>(bytes,ArquivoSistemaJson.Serializacao);ArquivoSistemaJson.Validar(restaurado);
  if(restaurado!.Gestao.Any(r=>r is null||r.Campos is null||CatalogoParoquial.Obter(r.Modulo) is null))throw new InvalidDataException("Backup com registros inválidos.");
  Directory.CreateDirectory(Pasta);File.Copy(arquivo.Caminho,Path.Combine(Pasta,"antes-restauracao-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+".json"));
  sessao.Documento.Rifas=restaurado.Rifas;sessao.Documento.Gestao=restaurado.Gestao;sessao.Documento.Eventos=restaurado.Eventos;sessao.Documento.EventosLegadosImportados=restaurado.EventosLegadosImportados;sessao.Documento.SequenciaDizimista=restaurado.SequenciaDizimista;
  sessao.Comunidades.Clear();sessao.Comunidades.AddRange(restaurado.Comunidades.Select(MapeadorCadastrosJson.ParaEntidade));
  sessao.Dizimistas.Clear();sessao.Dizimistas.AddRange(restaurado.Dizimistas.Select(MapeadorCadastrosJson.ParaEntidade));
  // Mantém contas atuais e histórico de auditoria para não reativar senhas e acessos antigos.
  Registrar(operador,"Restauração de backup","backups",nome);await sessao.SalvarAsync(ct);
 }
}
