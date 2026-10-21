using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Infraestrutura.Services.Implementation;
using UglyToad.PdfPig;
namespace SaoDimas.Tests.Infraestrutura;

public sealed class RifaPdfTests
{
    private static RifaPdfService Servico() => new(Options.Create(new ConfiguracaoParoquia { Nome = "Paróquia teste", CaminhoLogoImpressao = "wwwroot/img/brasao-impressao.png" }), new AmbienteDeTeste { ContentRootPath = Caminhos.RaizMvc() });
    [Fact]
    public void Quatro_bilhetes_maiores_cabem_em_uma_folha_A4()
    {
        using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(), "Capela teste", 1, 4));
        Assert.Single(pdf.GetPages());
        Assert.Equal(4, pdf.GetPage(1).NumberOfImages);
        Assert.Contains("0004", pdf.GetPage(1).Text, StringComparison.Ordinal);
    }

    private static RifaDto Rifa(string premios = "Bicicleta\nCesta") => new(Guid.NewGuid(), "Rifa beneficente teste", premios, new DateOnly(2026, 12, 10), 10m, 10, 1, "Aberta", 1, [], []);
    [Fact]
    public void Modelo_com_lista_de_cesta_preserva_quatro_bilhetes_e_canhoto_a_esquerda()
    {
        const string premios = "Panetone, Espumante, Vinho, Palmito, Azeitona, Azeite, Pêssego em lata, Ameixa em calda, Frutas cristalizadas, Castanha, Nozes, Avelã, Queijo bola, Bacalhau, Chester, Tender, Amendoim, Biscoito champanhe, Caixa de bombom e Frutas.";
        using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(premios), "Paróquia teste", 1, 4));
        var pagina = Assert.Single(pdf.GetPages());
        Assert.InRange(pagina.Width * 25.4 / 72, 209, 211);
        Assert.InRange(pagina.Height * 25.4 / 72, 296, 298);
        Assert.Equal(4, pagina.NumberOfImages);
        Assert.Equal(8, pagina.GetWords().Count(p => p.Text is "0001" or "0002" or "0003" or "0004"));
        Assert.All(pagina.GetWords().Where(p => p.Text == "CANHOTO"), p => Assert.True(p.BoundingBox.Left < pagina.Width / 3));
        Assert.Equal(4, pagina.GetWords().Count(p => p.Text == "Frutas."));
    }
    [Fact] public void Pdf_inclui_numeros_livres_e_canhotos_sem_reservas() { using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(), "Paróquia teste", 1, 10)); Assert.Equal(3, pdf.NumberOfPages); Assert.All(pdf.GetPages(), pagina => Assert.True(pagina.NumberOfImages > 0)); var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text)); foreach (var numero in Enumerable.Range(1, 10)) Assert.Contains(numero.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), texto, StringComparison.Ordinal); Assert.Contains("CANHOTO", texto, StringComparison.Ordinal); Assert.Contains("Bicicleta", texto, StringComparison.Ordinal); }
    [Fact] public void Intervalo_preserva_numeracao_e_dados_do_comprador() { var r = Rifa() with { Numeros = [new(6, "Ana teste", "11999999999", "Equipe teste", true, "PIX", DateTime.UtcNow, DateTime.UtcNow)] }; using var pdf = PdfDocument.Open(Servico().Gerar(r, "Capela teste", 5, 6)); Assert.Single(pdf.GetPages()); var texto = pdf.GetPage(1).Text; Assert.Contains("0005", texto, StringComparison.Ordinal); Assert.Contains("0006", texto, StringComparison.Ordinal); Assert.DoesNotContain("0007", texto, StringComparison.Ordinal); Assert.Contains("Ana teste", texto, StringComparison.Ordinal); }
    [Fact] public void Todos_os_20_premios_longos_sao_impressos() { var premios = string.Join('\n', Enumerable.Range(1, 20).Select(i => "Prêmio " + i + " " + new string('A', 65))); using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(premios), "Capela teste", 1, 1)); Assert.Single(pdf.GetPages()); Assert.Matches(@"Prêmio\s*20", pdf.GetPage(1).Text); }
    [Theory][InlineData(0, 1)][InlineData(5, 4)][InlineData(1, 11)] public void Intervalo_invalido_e_recusado(int inicio, int fim) => Assert.Throws<ArgumentOutOfRangeException>(() => Servico().Gerar(Rifa(), "Teste", inicio, fim));
}
