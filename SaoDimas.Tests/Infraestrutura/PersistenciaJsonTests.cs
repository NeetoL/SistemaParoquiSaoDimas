using SaoDimas.CrossCutting;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaoDimas.Aplicacao;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Enums;
using SaoDimas.Infraestrutura;
using SaoDimas.Infraestrutura.Persistencia.Json;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;

namespace SaoDimas.Tests.Infraestrutura;
public sealed partial class PersistenciaJsonTests : IDisposable
{
    private readonly string _diretorio = Path.Combine(Path.GetTempPath(), "saodimas-json-" + Guid.NewGuid().ToString("N"));
    private ServiceProvider Provider() => CriarProvider(_diretorio);
    private static ServiceProvider CriarProvider(string diretorio)
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistencia:Diretorio"] = diretorio,
            ["Persistencia:CadastrosIniciais"] = Path.Combine(diretorio, "originais.json"),
            ["Login:Usuario"] = "admin",
            ["Login:SenhaHash"] = "AQAAAAIAAYagAAAAEIdNimkd2Cqjw3koU2AtNqIHaoOflrgNQ+AMjN51zpGGb0SrpnekIG78vVxYLMzBOA==",
            ["Paroquia:Nome"] = "Paróquia São Dimas",
            ["Paroquia:CaminhoLogoImpressao"] = "wwwroot/img/brasao-impressao.png"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new AmbienteDeTeste { ContentRootPath = Caminhos.RaizMvc() });
        services.AddAplicacao().AddInfraestrutura(configuracao).AddCrossCutting(configuracao);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
    private static async Task<T> Requisicao<S, T>(ServiceProvider provider, Func<S, Task<T>> acao, string? legado = null) where S : notnull
    {
        using var escopo = provider.CreateScope();
        var estado = escopo.ServiceProvider.GetRequiredService<IEstadoEventosServidor>();
        Assert.True(await estado.CarregarAsync(legado, CancellationToken.None));
        var resultado = await acao(escopo.ServiceProvider.GetRequiredService<S>());
        await estado.SalvarAsync(CancellationToken.None);
        return resultado;
    }
    private static DadosDizimista Dados(string nome = "João da Silva", string? cpf = null) => new(nome, cpf, "(21) 99999-1234", 2,
        new DateOnly(2026, 1, 1), StatusDizimista.Ativo, "Rua das Flores, 10", "21775-280", "Padre Miguel", new DateOnly(1990, 7, 12), CodigoOriginal: "9001");

    [Fact]
    public async Task Codigo_original_sobrevive_a_edicao_e_reinicio_e_aparece_na_busca_e_envelope()
    {
        int id;
        using (var provider = Provider())
            id = (await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider, app => app.CadastrarAsync(Dados(), CancellationToken.None))).Valor;
        var caminho = Path.Combine(_diretorio, "sistema.json");
        var documento = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(caminho, TestContext.Current.CancellationToken))!;
        documento["dizimistas"]!.AsArray().Single(p => p!["id"]!.GetValue<int>() == id)!["codigoOriginal"] = null;
        await File.WriteAllTextAsync(caminho, documento.ToJsonString(), TestContext.Current.CancellationToken);
        var originais = documento.DeepClone();
        var pessoa = originais["dizimistas"]!.AsArray().Single(p => p!["id"]!.GetValue<int>() == id)!;
        pessoa["codigoOriginal"] = "042";
        pessoa["id"] = 500;
        await File.WriteAllTextAsync(Path.Combine(_diretorio, "originais.json"), originais.ToJsonString(), TestContext.Current.CancellationToken);
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(provider, app => app.ObterDetalhesAsync(id, CancellationToken.None));
            Assert.Equal("042", detalhes!.Codigo);
            var busca = await Requisicao<IDizimistaAplicacao, PaginaResultado<DizimistaResumoDto>>(provider, app => app.PesquisarAsync(new() { Busca = "42" }, CancellationToken.None));
            Assert.Equal(id, Assert.Single(busca.Itens).Id);
            var arquivo = await Requisicao<IEnvelopeDizimoAplicacao, Resultado<ArquivoPdf>>(provider, app => app.GerarAsync(id, CancellationToken.None));
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(arquivo.Valor.Conteudo);
            var texto = string.Concat(pdf.GetPage(1).Letters.Select(l => l.Value));
            Assert.Contains("042", texto, StringComparison.Ordinal);
            Assert.DoesNotContain(SaoDimas.Dominio.Entities.Dizimista.FormatarCodigo(id), texto, StringComparison.Ordinal);
            Assert.True((await Requisicao<IDizimistaAplicacao, Resultado>(provider, app => app.AtualizarAsync(id, Dados("Maria") with { CodigoOriginal = "042" }, CancellationToken.None))).Sucesso);
        }
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(provider, app => app.ObterDetalhesAsync(id, CancellationToken.None));
            Assert.Equal("042", detalhes!.Codigo);
            Assert.Equal(id, detalhes.Id);
            Assert.Equal("Maria", detalhes.Nome);
        }
    }

    [Fact]
    public async Task Cadastro_edicao_consultas_e_envelope_sobrevivem_ao_reinicio_sem_sql()
    {
        int id;
        using (var provider = Provider())
        {
            var resultado = await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider, app => app.CadastrarAsync(Dados(), CancellationToken.None));
            Assert.True(resultado.Sucesso); id = resultado.Valor;
        }
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(provider, app => app.ObterDetalhesAsync(id, CancellationToken.None));
            Assert.Equal("21775280", detalhes!.Cep);
            Assert.Equal(new DateOnly(1990, 7, 12), detalhes.DataNascimento);
            var pagina = await Requisicao<IDizimistaAplicacao, PaginaResultado<DizimistaResumoDto>>(provider, app =>
                app.PesquisarAsync(new FiltroDizimistas { Busca = "joao" }, CancellationToken.None));
            Assert.Equal(id, Assert.Single(pagina.Itens).Id);
            var pdf = await Requisicao<IEnvelopeDizimoAplicacao, Resultado<ArquivoPdf>>(provider, app => app.GerarAsync(id, CancellationToken.None));
            Assert.True(pdf.Sucesso);
            Assert.True((await Requisicao<IDizimistaAplicacao, Resultado>(provider, app => app.AtualizarAsync(id, Dados("Maria"), CancellationToken.None))).Sucesso);
        }
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(provider, app => app.ObterDetalhesAsync(id, CancellationToken.None));
            Assert.Equal("Maria", detalhes!.Nome);
            Assert.True(File.Exists(Path.Combine(_diretorio, "sistema.json.bak")));
        }
    }

    [Fact]
    public async Task Eventos_lotes_faixas_e_caixa_sobrevivem_ao_reinicio_e_ignoram_documentos_antigos()
    {
        int evento, produto, lote;
        using (var provider = Provider())
        {
            evento = (await Requisicao<IEventoAplicacao, Resultado<int>>(provider, app => app.CriarAsync(
                new DadosEvento("Festa JSON", null, 2, new DateOnly(2026, 10, 10), null, null), CancellationToken.None))).Valor;
            produto = (await Requisicao<IEventoAplicacao, Resultado<int>>(provider, app => app.AdicionarProdutoAsync(evento,
                new DadosProduto("Feijoada", null, 25m, true), CancellationToken.None))).Valor;
            lote = (await Requisicao<ITicketAplicacao, Resultado<int>>(provider, app => app.GerarLoteAsync(evento, produto, 100, CancellationToken.None))).Valor;
            Assert.True((await Requisicao<ITicketAplicacao, Resultado>(provider, app => app.DistribuirAsync(evento, lote,
                new DadosDistribuicao(1, 20, "Ana"), CancellationToken.None))).Sucesso);
            Assert.True((await Requisicao<IGestaoEventoAplicacao, Resultado>(provider, app => app.AbrirAsync(evento, 50m, "admin", null, CancellationToken.None))).Sucesso);
        }
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<ITicketAplicacao, LoteDetalhesDto?>(provider,
                app => app.ObterLoteAsync(evento, lote, CancellationToken.None), "{ documento adulterado }");
            Assert.Equal(100, detalhes!.Quantidade); Assert.Equal(20, detalhes.Distribuidos);
            Assert.Contains(detalhes.Faixas, f => f.Responsavel == "Ana");
            using var escopo = provider.CreateScope();
            var estado = escopo.ServiceProvider.GetRequiredService<IEstadoEventosServidor>();
            Assert.True(await estado.CarregarAsync(null, CancellationToken.None));
            var salvo = escopo.ServiceProvider.GetRequiredService<SessaoSistemaJson>().Documento;
            Assert.NotNull(salvo.Eventos.Eventos[0].Caixa);
        }
    }

    [Fact]
    public async Task Gravacoes_concorrentes_em_duas_instancias_preservam_todos_os_registros_e_ids()
    {
        using var primeiro = Provider(); using var segundo = Provider();
        var tarefas = Enumerable.Range(0, 12).Select(i => Requisicao<IDizimistaAplicacao, Resultado<int>>(i % 2 == 0 ? primeiro : segundo,
            app => app.CadastrarAsync(Dados($"Pessoa {i}") with { CodigoOriginal = $"TESTE-{i}" }, CancellationToken.None)));
        var resultados = await Task.WhenAll(tarefas);
        Assert.All(resultados, r => Assert.True(r.Sucesso));
        Assert.Equal(12, resultados.Select(r => r.Valor).Distinct().Count());
        using var reiniciado = Provider();
        var pagina = await Requisicao<IDizimistaAplicacao, PaginaResultado<DizimistaResumoDto>>(reiniciado,
            app => app.PesquisarAsync(new FiltroDizimistas(), CancellationToken.None));
        Assert.Equal(12, pagina.Itens.Count);
    }

    [Fact]
    public async Task Consulta_global_de_envelopes_inclui_apenas_ativos_de_todas_as_comunidades()
    {
        using var provider = Provider();
        foreach (var dados in new[] {
            Dados("Zelia") with { ComunidadeId = 1, CodigoOriginal = "A1" },
            Dados("Ana") with { ComunidadeId = 2, CodigoOriginal = "A2" },
            Dados("Inativo") with { ComunidadeId = 3, CodigoOriginal = "A3", Status = StatusDizimista.Inativo } })
            Assert.True((await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider,
                app => app.CadastrarAsync(dados, CancellationToken.None))).Sucesso);
        var ativos = await Requisicao<SaoDimas.Aplicacao.Queries.Interface.IDizimistaConsultas, IReadOnlyList<DizimistaIdentificacaoDto>>(provider,
            consulta => consulta.ListarIdentificacoesAtivosAsync(null, CancellationToken.None));
        Assert.Equal(["Ana", "Zelia"], ativos.Select(d => d.Nome));
        Assert.Equal(["A2", "A1"], ativos.Select(d => d.Codigo));
    }

    [Fact]
    public async Task Codigo_duplicado_e_recusado_em_duas_instancias_concorrentes()
    {
        using var primeiro = Provider(); using var segundo = Provider();
        var resultados = await Task.WhenAll(new[] { primeiro, segundo }.Select((provider, i) =>
            Requisicao<IDizimistaAplicacao, Resultado<int>>(provider,
                app => app.CadastrarAsync(Dados($"Pessoa {i}") with { CodigoOriginal = "0057" }, CancellationToken.None))));
        Assert.Single(resultados, r => r.Sucesso);
        Assert.Equal(nameof(DadosDizimista.CodigoOriginal), Assert.Single(Assert.Single(resultados, r => !r.Sucesso).Erros).Campo);
        using var reiniciado = Provider();
        var pagina = await Requisicao<IDizimistaAplicacao, PaginaResultado<DizimistaResumoDto>>(reiniciado,
            app => app.PesquisarAsync(new FiltroDizimistas(), CancellationToken.None));
        Assert.Equal("0057", Assert.Single(pagina.Itens).Codigo);
    }

    [Fact]
    public async Task Cpf_duplicado_e_recusado_mesmo_em_gravacoes_concorrentes()
    {
        using var provider = Provider();
        var resultados = await Task.WhenAll(Enumerable.Range(0, 3).Select(i =>
            Requisicao<IDizimistaAplicacao, Resultado<int>>(provider, app => app.CadastrarAsync(Dados($"Pessoa {i}", "12345678909"), CancellationToken.None))));
        Assert.Single(resultados, r => r.Sucesso);
    }

    [Fact]
    public async Task Documento_corrompido_nao_e_substituido_por_arquivo_vazio()
    {
        using var provider = Provider();
        await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider, app => app.CadastrarAsync(Dados(), CancellationToken.None));
        var caminho = Path.Combine(_diretorio, "sistema.json");
        await File.WriteAllTextAsync(caminho, "{ corrompido", TestContext.Current.CancellationToken);
        for (var i = 0; i < 2; i++)
            await Assert.ThrowsAsync<JsonException>(() => Requisicao<IDizimistaAplicacao, PaginaResultado<DizimistaResumoDto>>(provider,
                app => app.PesquisarAsync(new FiltroDizimistas(), CancellationToken.None)));
        Assert.Equal("{ corrompido", await File.ReadAllTextAsync(caminho, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(caminho + ".bak"));
    }

    [Fact]
    public async Task Eventos_do_navegador_sao_importados_uma_vez_sem_substituir_o_servidor()
    {
        var legado = JsonSerializer.Serialize(new DocumentoEventos
        {
            Sequencias = new SequenciasRegistro { Evento = 40 },
            Eventos = [new EventoRegistro { Id = 40, Nome = "Festa legada", ComunidadeId = 2, Status = 1,
                DataInicio = new DateOnly(2026, 10, 10), CriadoEm = DateTime.UtcNow }]
        }, ArquivoSistemaJson.Serializacao);
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(provider, app => app.ObterDetalhesAsync(40, CancellationToken.None), legado);
            Assert.Equal("Festa legada", detalhes!.Nome);
        }
        using (var provider = Provider())
        {
            var resultado = await Requisicao<IEventoAplicacao, Resultado<int>>(provider, app => app.CriarAsync(
                new DadosEvento("Festa nova", null, 1, new DateOnly(2026, 10, 10), null, null), CancellationToken.None), legado);
            Assert.Equal(41, resultado.Valor);
        }
    }
    public void Dispose()
    {
        if (Directory.Exists(_diretorio)) Directory.Delete(_diretorio, recursive: true);
    }
}
