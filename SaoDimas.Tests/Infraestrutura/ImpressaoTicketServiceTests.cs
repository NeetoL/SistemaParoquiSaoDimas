using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Infraestrutura.Services.Implementation;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Tickets = SaoDimas.Infraestrutura.Services.Implementation.ImpressaoTicketService;

namespace SaoDimas.Tests.Infraestrutura;

public sealed class ImpressaoTicketServiceTests
{
    private const double PontosPorMm = 72 / 25.4;

    private static ImpressaoTicketService CriarServico() => new(
        Options.Create(new ConfiguracaoParoquia { Nome = "Paróquia São Dimas", CaminhoLogoImpressao = "wwwroot/img/brasao-impressao.png" }),
        new AmbienteDeTeste { ContentRootPath = Caminhos.RaizMvc() });

    /// <summary>Tickets 1–12 do João, 13–24 da Maria e 25 sem responsável.</summary>
    private static ImpressaoTicketsDto Impressao() => new(
        "Capela Santa Teresinha",
        "Festa de Santa Teresinha 2026",
        "Feijoada",
        25.00m,
        Enumerable.Range(1, 25).Select(numero => new TicketImpressaoDto(numero, numero switch
        {
            <= 12 => "João da Silva",
            <= 24 => "Maria Souza",
            _ => null
        })).ToList());

    [Fact]
    public void Gera_A4_com_12_tickets_por_folha()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar(Impressao()));

        Assert.Equal(3, pdf.NumberOfPages);
        var pagina = pdf.GetPage(1);
        Assert.Equal(210, pagina.Width / PontosPorMm, precision: 1);
        Assert.Equal(297, pagina.Height / PontosPorMm, precision: 1);
    }

    [Fact]
    public void Cada_ticket_tem_o_numero_na_parte_do_cliente_e_no_canhoto()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar(Impressao()));
        var palavras = pdf.GetPage(2).GetWords().Select(palavra => palavra.Text).ToList();

        Assert.Equal(2, palavras.Count(palavra => palavra == "000015"));
        Assert.Contains("PARÓQUIA", palavras);
        Assert.Contains("FEIJOADA", palavras);
        Assert.Contains("CONTROLE", palavras);
        Assert.Contains("R$", string.Join(' ', palavras), StringComparison.Ordinal);
    }

    [Fact]
    public void Canhoto_mostra_o_responsavel_da_faixa_do_numero()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar(Impressao()));

        Assert.Equal(12, Ocorrencias(pdf.GetPage(1), "João"));
        Assert.Equal(0, Ocorrencias(pdf.GetPage(1), "Maria"));
        Assert.Equal(12, Ocorrencias(pdf.GetPage(2), "Maria"));
        Assert.Equal(0, Ocorrencias(pdf.GetPage(3), "João") + Ocorrencias(pdf.GetPage(3), "Maria"));
    }

    [Fact]
    public void Nenhum_texto_cruza_as_linhas_de_corte_nem_a_linha_de_destaque()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar(Impressao()));
        var pagina = pdf.GetPage(1);

        var cortesVerticais = Enumerable.Range(0, Tickets.Colunas + 1).Select(c => Tickets.MargemEsquerda + (c * Tickets.LarguraTicket))
            .Concat(Enumerable.Range(0, Tickets.Colunas).Select(c => Tickets.MargemEsquerda + (c * Tickets.LarguraTicket) + Tickets.LarguraCliente))
            .ToList();
        var cortesHorizontais = Enumerable.Range(0, Tickets.Linhas + 1).Select(l => Tickets.MargemTopo + (l * Tickets.AlturaTicket)).ToList();

        var cruzando = pagina.Letters
            .Select(letra => (letra.Value, Esq: letra.BoundingBox.Left / PontosPorMm, Dir: letra.BoundingBox.Right / PontosPorMm,
                Topo: (pagina.Height - letra.BoundingBox.Top) / PontosPorMm, Base: (pagina.Height - letra.BoundingBox.Bottom) / PontosPorMm))
            .Where(letra => letra.Topo < Tickets.MargemTopo + (Tickets.Linhas * Tickets.AlturaTicket)) // ignora o rodapé
            .Where(letra => cortesVerticais.Any(x => letra.Esq < x && letra.Dir > x)
                            || cortesHorizontais.Any(y => letra.Topo < y && letra.Base > y))
            .Select(letra => letra.Value)
            .ToList();

        Assert.Empty(cruzando);
    }

    private static int Ocorrencias(Page pagina, string palavra) =>
        pagina.GetWords().Count(item => item.Text == palavra);
}
