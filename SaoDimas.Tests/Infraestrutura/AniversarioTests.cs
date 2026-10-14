using System.Text.Json.Nodes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Tests.Infraestrutura;

public sealed partial class PersistenciaJsonTests
{
    [Theory]
    [InlineData("20/11", true)]
    [InlineData("20/11/", true)]
    [InlineData("29/02", true)]
    [InlineData("31/04", false)]
    [InlineData("00/12", false)]
    [InlineData("12/13", false)]
    [InlineData("1990-11-20", true)]
    public void Aniversario_valida_dia_mes_e_aceita_formato_legado(string texto, bool valido)
    {
        Assert.Equal(valido, Aniversario.TryParse(texto, out _));
    }

    [Fact]
    public async Task Aniversario_sem_ano_sobrevive_ao_reinicio_e_data_legada_e_convertida()
    {
        int id;
        using (var provider = Provider())
            id = (await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider,
                app => app.CadastrarAsync(Dados() with { DataNascimento = new Aniversario(29, 2) }, CancellationToken.None))).Valor;
        var caminho = Path.Combine(_diretorio, "sistema.json");
        var documento = JsonNode.Parse(await File.ReadAllTextAsync(caminho, TestContext.Current.CancellationToken))!;
        var pessoa = documento["dizimistas"]!.AsArray().Single(p => p!["id"]!.GetValue<int>() == id)!;
        Assert.Equal("29/02", pessoa["dataNascimento"]!.GetValue<string>());
        pessoa["dataNascimento"] = "1980-11-20";
        await File.WriteAllTextAsync(caminho, documento.ToJsonString(), TestContext.Current.CancellationToken);
        using (var provider = Provider())
        {
            var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(provider,
                app => app.ObterDetalhesAsync(id, CancellationToken.None));
            Assert.Equal(new Aniversario(20, 11), detalhes!.DataNascimento);
            var arquivo = await Requisicao<IEnvelopeDizimoAplicacao, Resultado<ArquivoPdf>>(provider,
                app => app.GerarAsync(id, CancellationToken.None));
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(arquivo.Valor.Conteudo);
            var texto = string.Concat(pdf.GetPage(1).Letters.Select(l => l.Value));
            Assert.Contains("20/11", texto, StringComparison.Ordinal);
            Assert.DoesNotContain("1980", texto, StringComparison.Ordinal);
            Assert.True((await Requisicao<IDizimistaAplicacao, Resultado>(provider,
                app => app.AtualizarAsync(id, Dados() with { DataNascimento = new Aniversario(20, 11) }, CancellationToken.None))).Sucesso);
        }
        documento = JsonNode.Parse(await File.ReadAllTextAsync(caminho, TestContext.Current.CancellationToken))!;
        Assert.Equal("20/11", documento["dizimistas"]!.AsArray().Single(p => p!["id"]!.GetValue<int>() == id)!["dataNascimento"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(null, "20/11")]
    [InlineData("12/07", "12/07")]
    public async Task Atualizacao_recupera_apenas_aniversarios_vazios_da_base_original(string? cadastrado, string esperado)
    {
        int id;
        using (var provider = Provider())
            id = (await Requisicao<IDizimistaAplicacao, Resultado<int>>(provider,
                app => app.CadastrarAsync(Dados() with { DataNascimento = cadastrado is null ? null : Aniversario.Parse(cadastrado) }, CancellationToken.None))).Valor;
        var documento = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(_diretorio, "sistema.json"), TestContext.Current.CancellationToken))!;
        documento["dizimistas"]!.AsArray().Single(p => p!["id"]!.GetValue<int>() == id)!["dataNascimento"] = "20/11";
        await File.WriteAllTextAsync(Path.Combine(_diretorio, "originais.json"), documento.ToJsonString(), TestContext.Current.CancellationToken);
        using var novoProvider = Provider();
        var detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(novoProvider,
            app => app.ObterDetalhesAsync(id, CancellationToken.None));
        Assert.Equal(Aniversario.Parse(esperado), detalhes!.DataNascimento);
        Assert.True((await Requisicao<IDizimistaAplicacao, Resultado>(novoProvider,
            app => app.AtualizarAsync(id, Dados() with { DataNascimento = null }, CancellationToken.None))).Sucesso);
        detalhes = await Requisicao<IDizimistaAplicacao, DizimistaDetalhesDto?>(novoProvider,
            app => app.ObterDetalhesAsync(id, CancellationToken.None));
        Assert.Null(detalhes!.DataNascimento);
    }

    [Fact]
    public async Task Csv_importa_e_exporta_aniversario_sem_ano()
    {
        using var provider = Provider();
        const string csv = "nome;comunidadeId;dataEntrada;status;dataNascimento;codigoOriginal\nPessoa teste;1;2026-01-01;Ativo;20/11/;9898\n";
        var resultado = await Requisicao<ICadastrosPlanilhaAplicacao, ResultadoGestao>(provider,
            app => app.ImportarAsync(csv, true, "teste", CancellationToken.None));
        Assert.True(resultado.Sucesso);
        var exportado = await Requisicao<ICadastrosPlanilhaAplicacao, string>(provider,
            app => app.ExportarAsync(CancellationToken.None));
        Assert.Contains("\"20/11\"", exportado, StringComparison.Ordinal);
    }
}
