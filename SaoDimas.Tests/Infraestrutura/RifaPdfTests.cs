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
    public void Seis_bilhetes_cabem_em_uma_folha_A4()
    {
        using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(), "Capela teste", 1, 6));
        Assert.Single(pdf.GetPages());
        Assert.Equal(6, pdf.GetPage(1).NumberOfImages);
        Assert.Contains("0006", pdf.GetPage(1).Text, StringComparison.Ordinal);
    }

    private static RifaDto Rifa(string premios = "Bicicleta\nCesta") => new(Guid.NewGuid(), "Rifa beneficente teste", premios, new DateOnly(2026, 12, 10), 10m, 10, 1, "Aberta", 1, [], []);
    [Fact] public void Pdf_inclui_numeros_livres_e_canhotos_sem_reservas() { using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(), "Paróquia teste", 1, 10)); Assert.Equal(2, pdf.NumberOfPages); Assert.All(pdf.GetPages(), pagina => Assert.True(pagina.NumberOfImages > 0)); var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text)); foreach (var numero in Enumerable.Range(1, 10)) Assert.Contains(numero.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), texto, StringComparison.Ordinal); Assert.Contains("CANHOTO", texto, StringComparison.Ordinal); Assert.Contains("Bicicleta", texto, StringComparison.Ordinal); }
    [Fact] public void Intervalo_preserva_numeracao_e_dados_do_comprador() { var r = Rifa() with { Numeros = [new(6, "Ana teste", "11999999999", "Equipe teste", true, "PIX", DateTime.UtcNow, DateTime.UtcNow)] }; using var pdf = PdfDocument.Open(Servico().Gerar(r, "Capela teste", 5, 6)); Assert.Single(pdf.GetPages()); var texto = pdf.GetPage(1).Text; Assert.Contains("0005", texto, StringComparison.Ordinal); Assert.Contains("0006", texto, StringComparison.Ordinal); Assert.DoesNotContain("0007", texto, StringComparison.Ordinal); Assert.Contains("Ana teste", texto, StringComparison.Ordinal); }
    [Fact] public void Todos_os_20_premios_longos_sao_impressos() { var premios = string.Join('\n', Enumerable.Range(1, 20).Select(i => "Prêmio " + i + " " + new string('A', 65))); using var pdf = PdfDocument.Open(Servico().Gerar(Rifa(premios), "Capela teste", 1, 1)); Assert.Single(pdf.GetPages()); Assert.Matches(@"Prêmio\s*20", pdf.GetPage(1).Text); }
    [Theory][InlineData(0, 1)][InlineData(5, 4)][InlineData(1, 11)] public void Intervalo_invalido_e_recusado(int inicio, int fim) => Assert.Throws<ArgumentOutOfRangeException>(() => Servico().Gerar(Rifa(), "Teste", inicio, fim));
}
