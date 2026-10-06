using SaoDimas.CrossCutting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaoDimas.Aplicacao;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura;
using UglyToad.PdfPig;

namespace SaoDimas.Tests.Infraestrutura;

/// <summary>
/// Fluxo completo do módulo pelo DI real e pela persistência temporária: cada "requisição" é um escopo novo que
/// recebe o documento salvo pela anterior (exatamente como o navegador faz com o localStorage).
/// </summary>
public sealed partial class EventosPersistenciaTemporariaTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private string? _documentoNoNavegador;

    public EventosPersistenciaTemporariaTests()
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SaoDimas"] = "Server=teste;Database=SaoDimas;Integrated Security=True",
                ["Paroquia:Nome"] = "Paróquia São Dimas",
                ["Paroquia:CaminhoLogoImpressao"] = "wwwroot/img/brasao-impressao.png"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new AmbienteDeTeste { ContentRootPath = Caminhos.RaizMvc() });
        services.AddAplicacao().AddInfraestrutura(configuracao).AddCrossCutting(configuracao);

        // Comunidades vêm do SQL Server em produção; aqui, um dublê com a Capela Santa Teresinha.
        services.AddScoped<IComunidadeRepositorio>(_ => new ComunidadesFake(Criar.Comunidade(2, nome: "Santa Teresinha")));

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task Fluxo_completo_sobrevive_entre_requisicoes()
    {
        var eventoId = (await Requisicao<IEventoAplicacao, Resultado<int>>(app => app.CriarAsync(
            new DadosEvento("Festa de Santa Teresinha 2026", null, 2, new DateOnly(2026, 9, 19), new DateOnly(2026, 9, 27), null),
            CancellationToken.None))).Valor;

        var produtoId = (await Requisicao<IEventoAplicacao, Resultado<int>>(app =>
            app.AdicionarProdutoAsync(eventoId, new DadosProduto("Feijoada", null, 25.00m, true), CancellationToken.None))).Valor;

        var loteId = (await Requisicao<ITicketAplicacao, Resultado<int>>(app =>
            app.GerarLoteAsync(eventoId, produtoId, 100, CancellationToken.None))).Valor;

        foreach (var (inicial, final, responsavel) in new[] { (1, 20, "João da Silva"), (21, 50, "Maria Souza"), (51, 75, "Carlos Santos") })
        {
            var distribuicao = await Requisicao<ITicketAplicacao, Resultado>(app =>
                app.DistribuirAsync(eventoId, loteId, new DadosDistribuicao(inicial, final, responsavel), CancellationToken.None));
            Assert.True(distribuicao.Sucesso);
        }

        var jose = await Requisicao<ITicketAplicacao, Resultado>(app =>
            app.DistribuirAsync(eventoId, loteId, new DadosDistribuicao(15, 30, "José"), CancellationToken.None));
        Assert.False(jose.Sucesso);

        var lote = await Requisicao<ITicketAplicacao, LoteDetalhesDto?>(app => app.ObterLoteAsync(eventoId, loteId, CancellationToken.None));
        Assert.NotNull(lote);
        Assert.Equal((100, 75, 25, 3, 2_500.00m), (lote.Quantidade, lote.Distribuidos, lote.Disponiveis, lote.Responsaveis, lote.ValorPotencial));
        Assert.Equal(
            ["001–020 João da Silva", "021–050 Maria Souza", "051–075 Carlos Santos", "076–100 "],
            lote.Faixas.Select(faixa => $"{faixa.Faixa} {faixa.Responsavel}"));
        Assert.Equal(TipoFaixaLote.Livre, lote.Faixas[^1].Tipo);

        var joaoId = lote.Faixas[0].DistribuicaoId!.Value;
        // Simula um documento da versão anterior; o caminho antigo de aplicação agora é bloqueado.
        var prestacao = await Requisicao<IEventoRepositorio, Resultado>(async repositorio =>
        {
            var evento = await repositorio.ObterPorIdAsync(eventoId, CancellationToken.None);
            using var escopoLote = _provider.CreateScope();
            var estadoLote = escopoLote.ServiceProvider.GetRequiredService<IEstadoEventosNavegador>();
            Assert.True(estadoLote.Carregar(_documentoNoNavegador));
            var repositorioLote = escopoLote.ServiceProvider.GetRequiredService<ILoteTicketRepositorio>();
            var loteLegado = await repositorioLote.ObterPorIdAsync(loteId, CancellationToken.None);
            var resultado = loteLegado!.RegistrarPrestacao(evento!, joaoId, 18, 2, 400m, "Dois tickets pagos no domingo.", RelogioFixo.Padrao);
            await repositorioLote.SalvarAlteracoesAsync(CancellationToken.None);
            _documentoNoNavegador = estadoLote.ObterDocumentoAlterado();
            return resultado;
        });
        Assert.True(prestacao.Sucesso);

        var detalhes = await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(app => app.ObterDetalhesAsync(eventoId, CancellationToken.None));
        Assert.NotNull(detalhes);
        Assert.Equal("Capela Santa Teresinha", detalhes.Comunidade);
        Assert.Equal((100, 75, 25), (detalhes.TicketsGerados, detalhes.TicketsDistribuidos, detalhes.TicketsDisponiveis));
        var prestada = Assert.Single(detalhes.Prestacoes, item => item.Prestacao is not null).Prestacao!;
        Assert.Equal((450.00m, 400.00m, -50.00m), (prestada.ValorEsperado, prestada.ValorEntregue, prestada.Diferenca));
        Assert.Equal((450.00m, -50.00m, 1, 2), (detalhes.TotaisPrestacao.ValorEsperado, detalhes.TotaisPrestacao.Diferenca,
            detalhes.TotaisPrestacao.FaixasPrestadas, detalhes.TotaisPrestacao.FaixasPendentes));

        // Faixa com prestação não pode ser removida; as outras sim.
        Assert.False((await Requisicao<ITicketAplicacao, Resultado>(app =>
            app.RemoverDistribuicaoAsync(eventoId, loteId, joaoId, CancellationToken.None))).Sucesso);

        // PDF da faixa do João: 20 tickets → 2 folhas, todos com o João no canhoto.
        var pdf = await Requisicao<ITicketAplicacao, Resultado<ArquivoPdf>>(app =>
            app.GerarPdfAsync(eventoId, loteId, joaoId, CancellationToken.None));
        Assert.Equal("tickets-feijoada-001-020.pdf", pdf.Valor.NomeArquivo);
        using var documento = PdfDocument.Open(pdf.Valor.Conteudo);
        Assert.Equal(2, documento.NumberOfPages);

        Assert.True((await Requisicao<IGestaoEventoAplicacao, Resultado>(app =>
            app.AbrirAsync(eventoId, 300m, "José", null, CancellationToken.None))).Sucesso);
        var migrado = (await Requisicao<IGestaoEventoAplicacao, GestaoEventoDto?>(app =>
            app.ObterAsync(eventoId, CancellationToken.None)))!;
        Assert.Equal(400m, migrado.Prestado); Assert.Equal(300m, migrado.DinheiroTeorico);
        Assert.Equal(FormaRecebimento.Outro, Assert.Single(migrado.Caixa.Movimentacoes).Forma);
        Assert.False((await Requisicao<IGestaoEventoAplicacao, Resultado>(app =>
            app.AbrirAsync(eventoId, 300m, "José", null, CancellationToken.None))).Sucesso);
    }

    [Fact]
    public async Task Documento_guarda_faixas_e_nao_os_tickets_individuais()
    {
        var eventoId = (await Requisicao<IEventoAplicacao, Resultado<int>>(app => app.CriarAsync(
            new DadosEvento("Feijoada", null, 2, new DateOnly(2026, 10, 10), null, null), CancellationToken.None))).Valor;
        var produtoId = (await Requisicao<IEventoAplicacao, Resultado<int>>(app =>
            app.AdicionarProdutoAsync(eventoId, new DadosProduto("Feijoada", null, 25m, true), CancellationToken.None))).Valor;

        await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(eventoId, produtoId, 5000, CancellationToken.None));

        // 5.000 tickets ocupam um único registro de lote (faixa 1–5000), não 5.000 registros.
        Assert.InRange(_documentoNoNavegador!.Length, 1, 2_000);
        Assert.Contains("\"numeroFinal\":5000", _documentoNoNavegador, StringComparison.Ordinal);
    }

    [Fact]
    public void Documento_invalido_e_recusado_sem_apagar_dados()
    {
        using var escopo = _provider.CreateScope();
        var estado = escopo.ServiceProvider.GetRequiredService<IEstadoEventosNavegador>();

        Assert.False(estado.Carregar("{ isto não é json"));
        Assert.False(estado.Carregar("{\"versao\":99}"));
        Assert.Null(estado.ObterDocumentoAlterado());
    }

    private async Task<TResultado> Requisicao<TServico, TResultado>(Func<TServico, Task<TResultado>> acao)
        where TServico : notnull
    {
        using var escopo = _provider.CreateScope();
        var estado = escopo.ServiceProvider.GetRequiredService<IEstadoEventosNavegador>();
        Assert.True(estado.Carregar(_documentoNoNavegador));

        var resultado = await acao(escopo.ServiceProvider.GetRequiredService<TServico>());

        _documentoNoNavegador = estado.ObterDocumentoAlterado() ?? _documentoNoNavegador;
        return resultado;
    }

    private sealed class ComunidadesFake(params Comunidade[] comunidades) : IComunidadeRepositorio
    {
        public Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(comunidades.FirstOrDefault(comunidade => comunidade.Id == id));

        public Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Comunidade>>(comunidades);
    }
}
