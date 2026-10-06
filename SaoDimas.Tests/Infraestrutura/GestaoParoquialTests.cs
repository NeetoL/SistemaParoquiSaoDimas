using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.Tests.Infraestrutura;
public sealed partial class PersistenciaJsonTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static Dictionary<string,string> Pastoral(string nome="Pastoral") => new() { ["nome"]=nome, ["responsavel"]="Ana", ["comunidade"]="1", ["telefone"]="", ["observacao"]="" };
    private static Dictionary<string,string> Agenda(string inicio="2026-10-10T10:00",string fim="2026-10-10T11:00") => new() { ["titulo"]="Encontro", ["tipo"]="Reunião", ["inicio"]=inicio, ["fim"]=fim, ["local"]="Salão", ["responsavel"]="Ana", ["comunidade"]="1", ["observacao"]="" };
    [Fact] public async Task Agenda_impede_conflitos_e_edicoes_desatualizadas()
    {
        using var p=Provider();
        var a=await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("agenda",null,0,Agenda(),"admin",null,null,Ct));
        Assert.True(a.Sucesso);
        Assert.False((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("agenda",null,0,Agenda("2026-10-10T10:30","2026-10-10T11:30"),"admin",null,null,Ct))).Sucesso);
        Assert.False((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("agenda",a.Id,2,Agenda(),"admin",null,null,Ct))).Sucesso);
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("agenda",null,0,Agenda("2026-10-10T11:00","2026-10-10T12:00"),"admin",null,null,Ct))).Sucesso);
    }
    [Fact] public async Task Csv_valida_lote_inteiro_antes_de_gravar_e_exporta_com_seguranca()
    {
        using var p=Provider();
        const string csv="nome;responsavel;comunidade;observacao\nPastoral A;Ana;1;\"Texto; com detalhe\"\nPastoral B;Ana;999;Erro\n";
        Assert.False((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync("pastorais",csv,"admin",true,Ct))).Sucesso);
        Assert.Empty(await Requisicao<IGestaoParoquialAplicacao,IReadOnlyList<RegistroParoquial>>(p,s=>s.ListarAsync("pastorais",null,null,null,null,Ct)));
        var valido=csv.Replace("999;Erro","1;Bom");
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync("pastorais",valido,"admin",false,Ct))).Sucesso);
        Assert.Empty(await Requisicao<IGestaoParoquialAplicacao,IReadOnlyList<RegistroParoquial>>(p,s=>s.ListarAsync("pastorais",null,null,null,null,Ct)));
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync("pastorais",valido,"admin",true,Ct))).Sucesso);
        Assert.Equal(2,(await Requisicao<IGestaoParoquialAplicacao,IReadOnlyList<RegistroParoquial>>(p,s=>s.ListarAsync("pastorais",null,null,null,null,Ct))).Count);
        Assert.False((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync("pastorais",valido,"admin",true,Ct))).Sucesso);
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("pastorais",null,0,Pastoral("=SUM(A1)"),"admin",null,null,Ct))).Sucesso);
        Assert.Contains("'=SUM(A1)",await Requisicao<IGestaoParoquialAplicacao,string>(p,s=>s.ExportarAsync("pastorais",null,null,null,Ct)));
    }
    [Fact] public async Task Backup_restaura_dados_preserva_contas_e_recusa_conteudo_invalido()
    {
        using var p=Provider();
        await Requisicao<IUsuariosAplicacao,IReadOnlyList<UsuarioParoquial>>(p,s=>s.ListarAsync(Ct));
        await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("pastorais",null,0,Pastoral(),"admin",null,null,Ct));
        var bytes=await Requisicao<IGestaoParoquialDados,byte[]>(p,s=>s.CriarBackupAsync(Ct));
        await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("pastorais",null,0,Pastoral("Outra"),"admin",null,null,Ct));
        var nome=await Requisicao<IGestaoParoquialDados,string>(p,s=>s.ReceberBackupAsync(bytes,Ct));
        await Requisicao<IGestaoParoquialDados,bool>(p,async s=>{await s.RestaurarBackupAsync(nome,"admin",Ct);return true;});
        Assert.Single(await Requisicao<IGestaoParoquialAplicacao,IReadOnlyList<RegistroParoquial>>(p,s=>s.ListarAsync("pastorais",null,null,null,null,Ct)));
        Assert.Single(await Requisicao<IUsuariosAplicacao,IReadOnlyList<UsuarioParoquial>>(p,s=>s.ListarAsync(Ct)));
        await Assert.ThrowsAsync<InvalidDataException>(()=>Requisicao<IGestaoParoquialDados,string>(p,s=>s.ReceberBackupAsync("{}"u8.ToArray(),Ct)));
        await Assert.ThrowsAsync<InvalidDataException>(()=>Requisicao<IGestaoParoquialDados,byte[]>(p,s=>s.LerBackupAsync("../sistema.json",Ct)));
        Assert.Contains(await Requisicao<IGestaoParoquialDados,IReadOnlyList<AuditoriaParoquial>>(p,s=>s.AuditoriaAsync(Ct)),a=>a.Acao.Contains("Restaur"));
    }
    [Fact] public async Task Ultimo_administrador_nao_pode_ser_desativado_e_senhas_sao_hash()
    {
        using var p=Provider();
        var admin=Assert.Single(await Requisicao<IUsuariosAplicacao,IReadOnlyList<UsuarioParoquial>>(p,s=>s.ListarAsync(Ct)));
        Assert.NotEqual("admin",admin.SenhaHash);
        Assert.False((await Requisicao<IUsuariosAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync(admin.Id,admin.Login,admin.Nome,"Administrador",false,null,"admin",Ct))).Sucesso);
        Assert.True((await Requisicao<IUsuariosAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync(null,"secretaria","Maria","Secretaria",true,"Senha123!","admin",Ct))).Sucesso);
        Assert.NotNull(await Requisicao<IUsuariosAplicacao,UsuarioParoquial?>(p,s=>s.AutenticarAsync("secretaria","Senha123!",Ct)));
        Assert.Null(await Requisicao<IUsuariosAplicacao,UsuarioParoquial?>(p,s=>s.AutenticarAsync("secretaria","errada",Ct)));
    }

    [Fact] public async Task Contribuicao_entra_no_saldo_uma_vez_e_cancelamento_preserva_historico()
    {
        using var p=Provider();
        var pessoa=await Requisicao<IDizimistaAplicacao,SaoDimas.Dominio.Resultado<int>>(p,s=>s.CadastrarAsync(Dados(),Ct));
        var campos=new Dictionary<string,string>{["dizimista"]=pessoa.Valor.ToString(System.Globalization.CultureInfo.InvariantCulture),["data"]="2026-10-06",["competencia"]="2026-10",["valor"]="100,50",["forma"]="PIX",["tipo"]="Dízimo",["comunidade"]="2"};
        var contribuicao=await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("contribuicoes",null,0,campos,"admin",null,null,Ct));
        Assert.True(contribuicao.Sucesso);
        var despesa=new Dictionary<string,string>{["descricao"]="Material",["data"]="2026-10-06",["natureza"]="Saída",["categoria"]="Materiais",["valor"]="40.00",["forma"]="Dinheiro",["comunidade"]="2"};
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("financeiro",null,0,despesa,"admin",null,null,Ct))).Sucesso);
        var resumo=await Requisicao<IGestaoParoquialAplicacao,ResumoFinanceiro>(p,s=>s.ResumoAsync(null,null,null,Ct));
        Assert.Equal(100.50m,resumo.Entradas);Assert.Equal(40m,resumo.Saidas);Assert.Equal(60.50m,resumo.Saldo);
        Assert.False((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.SalvarAsync("contribuicoes",contribuicao.Id,1,campos,"admin",null,null,Ct))).Sucesso);
        Assert.True((await Requisicao<IGestaoParoquialAplicacao,ResultadoGestao>(p,s=>s.CancelarAsync(contribuicao.Id!.Value,1,"admin",Ct))).Sucesso);
        Assert.Equal(-40m,(await Requisicao<IGestaoParoquialAplicacao,ResumoFinanceiro>(p,s=>s.ResumoAsync(null,null,null,Ct))).Saldo);
        Assert.Contains(await Requisicao<IGestaoParoquialDados,IReadOnlyList<RegistroParoquial>>(p,s=>s.RegistrosAsync(Ct)),r=>r.Id==contribuicao.Id&&r.Cancelado);
    }
    [Fact] public async Task Planilha_dizimistas_valida_antes_de_importar_e_rejeita_cpf_duplicado()
    {
        using var p=Provider();
        const string csv="nome;cpf;telefone;comunidadeId;dataEntrada;status;endereco;cep;bairro;dataNascimento;codigoOriginal\nJoão;12345678909;21999991234;2;2026-01-01;Ativo;Rua A;21775280;Centro;1990-08-19;9001\n";
        Assert.True((await Requisicao<ICadastrosPlanilhaAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync(csv,false,"admin",Ct))).Sucesso);
        var pagina=await Requisicao<IDizimistaAplicacao,PaginaResultado<DizimistaResumoDto>>(p,s=>s.PesquisarAsync(new(),Ct));Assert.Equal(0,pagina.Total);
        Assert.True((await Requisicao<ICadastrosPlanilhaAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync(csv,true,"admin",Ct))).Sucesso);
        Assert.False((await Requisicao<ICadastrosPlanilhaAplicacao,ResultadoGestao>(p,s=>s.ImportarAsync(csv,true,"admin",Ct))).Sucesso);
        Assert.Contains("João",await Requisicao<ICadastrosPlanilhaAplicacao,string>(p,s=>s.ExportarAsync(Ct)));
        Assert.Equal(1,(await Requisicao<IDizimistaAplicacao,PaginaResultado<DizimistaResumoDto>>(p,s=>s.PesquisarAsync(new(),Ct))).Total);
    }
}
