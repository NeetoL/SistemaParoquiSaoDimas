namespace SaoDimas.Aplicacao.Dtos;
public sealed record RegistroParoquial(Guid Id,string Modulo,Dictionary<string,string> Campos,DateTime CriadoEm,string CriadoPor,int Revisao=1,bool Cancelado=false,string? AnexoNome=null,string? AnexoBase64=null);
public sealed record UsuarioParoquial(Guid Id,string Login,string Nome,string Perfil,bool Ativo,string SenhaHash,int Versao=1);
public sealed record AuditoriaParoquial(Guid Id,DateTime Data,string Operador,string Acao,string Modulo,string Referencia,Dictionary<string,string>? Antes=null,Dictionary<string,string>? Depois=null);
public sealed record BackupParoquial(string Nome,DateTime Data,long Tamanho);
public sealed record CampoParoquial(string Chave,string Nome,string Tipo="text",bool Obrigatorio=true,string[]? Opcoes=null);
public sealed record ModuloParoquial(string Chave,string Nome,string Descricao,string Icone,string[] Perfis,CampoParoquial[] Campos);
public sealed record ResultadoGestao(bool Sucesso,string Mensagem,Guid? Id=null);
public sealed record ResumoFinanceiro(decimal Entradas,decimal Saidas,decimal Saldo);
public static class CatalogoParoquial
{
 public static readonly string[] Perfis=["Administrador","Secretaria","Tesouraria","Coordenador"];
 public static readonly ModuloParoquial[] Modulos=[
 new("contribuicoes","Dízimo e contribuições","Recebimentos, recibos e histórico por dizimista.","hand-coins",["Administrador","Secretaria","Tesouraria"],[
 new("dizimista","Dizimista","dizimista"),new("data","Data do recebimento","date"),new("competencia","Competência","month"),new("valor","Valor (R$)","money"),new("forma","Forma de contribuição","select",true,["Dinheiro","PIX","Transferência","Cartão","Outro"]),new("tipo","Tipo","select",true,["Dízimo","Oferta","Doação"]),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("financeiro","Financeiro","Entradas, despesas, comprovantes e saldo. Contribuições são incluídas automaticamente.","wallet",["Administrador","Tesouraria"],[
 new("descricao","Descrição"),new("data","Data","date"),new("natureza","Natureza","select",true,["Entrada","Saída"]),new("categoria","Categoria","select",true,["Doações","Manutenção","Água e energia","Materiais","Serviços","Pastorais","Outros"]),new("valor","Valor (R$)","money"),new("forma","Forma de pagamento","select",true,["Dinheiro","PIX","Transferência","Cartão","Outro"]),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("agenda","Agenda paroquial","Missas, reuniões e reservas de espaços com conferência de horários.","calendar-clock",Perfis,[
 new("titulo","Título"),new("tipo","Atividade","select",true,["Missa","Reunião","Reserva de sala","Formação","Celebração","Outro"]),new("inicio","Início","datetime-local"),new("fim","Término","datetime-local"),new("local","Espaço / local"),new("responsavel","Responsável"),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("sacramentos","Secretaria e sacramentos","Registros de batismo, crisma e casamento, livros e documentos.","book-open",["Administrador","Secretaria"],[
 new("nome","Nome"),new("tipo","Sacramento","select",true,["Batismo","Crisma","Casamento"]),new("data","Data da celebração","date"),new("nascimento","Data de nascimento","date",false),new("filiacao","Filiação","text",false),new("conjuge","Cônjuge (casamento)","text",false),new("padrinhos","Padrinhos / testemunhas","text",false),new("celebrante","Celebrante"),new("livro","Livro"),new("folha","Folha"),new("termo","Termo"),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("pastorais","Pastorais","Organização das pastorais e seus responsáveis.","church",["Administrador","Secretaria","Coordenador"],[
 new("nome","Nome da pastoral"),new("responsavel","Responsável"),new("telefone","Telefone","tel",false),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("voluntarios","Voluntários","Membros das pastorais, contatos e disponibilidade.","heart-handshake",["Administrador","Secretaria","Coordenador"],[
 new("nome","Nome"),new("telefone","Telefone","tel"),new("email","E-mail","email",false),new("pastoral","Pastoral","pastoral"),new("disponibilidade","Disponibilidade","textarea"),new("comunidade","Comunidade","comunidade"),new("observacao","Observações","textarea",false)]),
 new("escalas","Escalas","Serviços, voluntários e horários sem sobreposição.","list-checks",["Administrador","Secretaria","Coordenador"],[
 new("servico","Serviço"),new("voluntario","Voluntário","voluntario"),new("inicio","Início","datetime-local"),new("fim","Término","datetime-local"),new("local","Local"),new("observacao","Observações","textarea",false)])];
 public static ModuloParoquial? Obter(string chave)=>Modulos.SingleOrDefault(m=>m.Chave==chave);
 public static bool Permite(string perfil,string modulo)=>Obter(modulo)?.Perfis.Contains(perfil)==true;
}
