using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.Infraestrutura.Services.Implementation;

internal sealed class RifaPdfService(IOptions<ConfiguracaoParoquia> opcoes, IHostEnvironment ambiente) : IRifaPdfService
{
    private readonly Lazy<Image> _logo = new(() => Image.FromFile(Path.Combine(ambiente.ContentRootPath, opcoes.Value.CaminhoLogoImpressao)));
    private static readonly CultureInfo Portugues = CultureInfo.GetCultureInfo("pt-BR");

    public byte[] Gerar(RifaDto rifa, string comunidade, int inicio, int fim)
    {
        ArgumentNullException.ThrowIfNull(rifa);
        if (inicio < 1 || fim < inicio || fim > rifa.Quantidade) throw new ArgumentOutOfRangeException(nameof(inicio));
        var ocupados = rifa.Numeros.ToDictionary(n => n.Numero);
        // Quatro bilhetes de 64 mm: canhoto à esquerda, participante à direita, como o modelo impresso.
        // Conteúdo excepcionalmente longo recebe uma área maior, sem omitir prêmios ou compradores.
        var porFolha = rifa.Premios.Length > 650 ? 1
            : rifa.Premios.Length > 330 || rifa.Nome.Length > 70 || comunidade.Length > 90
                || rifa.Numeros.Any(n => n.Comprador.Length > 40 || n.Vendedor.Length > 40) ? 2 : 4;
        var premios = string.Join("  ·  ", rifa.Premios.Split('\n').Select((p, i) => (i + 1).ToString(CultureInfo.InvariantCulture) + "º " + p));
        var data = rifa.DataSorteio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var valor = rifa.Valor.ToString("C", Portugues);
        return Document.Create(doc =>
        {
            foreach (var folha in Enumerable.Range(inicio, fim - inicio + 1).Chunk(porFolha)) doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(8, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));
                page.Header().PaddingBottom(3, Unit.Millimetre).Row(row =>
                {
                    row.RelativeItem().Text(comunidade).FontSize(8);
                    row.ConstantItem(125).AlignRight().Text("A4 · imprimir em 100%").FontSize(8);
                });
                page.Content().Column(col =>
                {
                    col.Spacing(2, Unit.Millimetre);
                    foreach (var numero in folha)
                    {
                        ocupados.TryGetValue(numero, out var comprador);
                        var codigo = numero.ToString("D4", CultureInfo.InvariantCulture);
                        col.Item().ShowEntire().Height(256f / porFolha, Unit.Millimetre).Border(0.8f).Row(row =>
                        {
                            row.RelativeItem().Padding(3, Unit.Millimetre).Column(c =>
                            {
                                c.Item().AlignCenter().Text("CANHOTO · ORGANIZAÇÃO").FontSize(7).SemiBold();
                                c.Item().PaddingTop(2).AlignCenter().Text(rifa.Nome).FontSize(11).Bold();
                                c.Item().PaddingVertical(3, Unit.Millimetre).AlignCenter().Border(0.7f).PaddingHorizontal(9).PaddingVertical(3).Text("Nº " + codigo).FontSize(15).Bold();
                                c.Item().Border(0.6f).Padding(4).Text("Nome: " + (comprador?.Comprador ?? "____________________")).FontSize(10);
                                c.Item().PaddingTop(2, Unit.Millimetre).Border(0.6f).Padding(4).Text("Tel.: " + (string.IsNullOrWhiteSpace(comprador?.Telefone) ? "_____________________" : comprador.Telefone)).FontSize(10);
                                c.Item().PaddingTop(2, Unit.Millimetre).AlignCenter().Text("Sorteio: " + data).FontSize(9).Bold();
                                c.Item().PaddingTop(2).AlignCenter().Text(valor).FontSize(10).Bold();
                            });
                            row.ConstantItem(0.7f).Background(Colors.Black);
                            row.RelativeItem(2).Padding(3, Unit.Millimetre).Column(c =>
                            {
                                c.Item().Row(h =>
                                {
                                    h.ConstantItem(13, Unit.Millimetre).Height(15, Unit.Millimetre).Image(_logo.Value).FitArea();
                                    h.RelativeItem().PaddingHorizontal(3, Unit.Millimetre).AlignMiddle().AlignCenter().Column(t =>
                                    {
                                        t.Item().Text(rifa.Nome).FontSize(14).Bold();
                                        t.Item().Text(valor).FontSize(13).Bold();
                                    });
                                });
                                c.Item().PaddingVertical(2, Unit.Millimetre).Row(r =>
                                {
                                    r.ConstantItem(35, Unit.Millimetre).Border(0.7f).Padding(4).AlignCenter().Text("Nº " + codigo).FontSize(13).Bold();
                                    r.Spacing(3, Unit.Millimetre);
                                    r.RelativeItem().Border(0.7f).Padding(4).AlignCenter().Text("Sorteio: " + data).FontSize(10).Bold();
                                });
                                c.Item().AlignCenter().Text("Prêmios: " + premios).FontSize(10).SemiBold();
                                if (!string.IsNullOrWhiteSpace(comprador?.Comprador))
                                    c.Item().PaddingTop(2).Text("Comprador: " + comprador.Comprador).FontSize(9);
                                c.Item().ExtendVertical().AlignBottom().Column(rodape =>
                                {
                                    rodape.Item().Text("Vendedor: " + (string.IsNullOrWhiteSpace(comprador?.Vendedor) ? "________________________________" : comprador.Vendedor)).FontSize(9);
                                    if (comprador is not null)
                                        rodape.Item().Text(comprador.Pago ? "Pagamento confirmado" : "Pagamento pendente").FontSize(8);
                                });
                            });
                        });
                    }
                });
                page.Footer().PaddingTop(2, Unit.Millimetre).Row(row =>
                {
                    row.RelativeItem().Text("Corte nas bordas. Destaque o canhoto na linha vertical.").FontSize(7);
                    row.ConstantItem(60).AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                });
            });
        }).GeneratePdf();
    }
}
