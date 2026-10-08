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

    public byte[] Gerar(RifaDto rifa, string comunidade, int inicio, int fim)
    {
        ArgumentNullException.ThrowIfNull(rifa);
        if (inicio < 1 || fim < inicio || fim > rifa.Quantidade) throw new ArgumentOutOfRangeException(nameof(inicio));
        var ocupados = rifa.Numeros.ToDictionary(n => n.Numero);
        var porFolha = rifa.Premios.Length > 600 ? 1 : rifa.Premios.Length > 200 || rifa.Nome.Length > 70 || rifa.Numeros.Any(n => n.Comprador.Length > 60 || n.Vendedor.Length > 60) ? 2 : 4;
        return Document.Create(doc =>
        {
            foreach (var folha in Enumerable.Range(inicio, fim - inicio + 1).Chunk(porFolha)) doc.Page(page =>
            {
                page.Size(PageSizes.A4); page.Margin(10, Unit.Millimetre); page.DefaultTextStyle(t => t.FontSize(9).FontColor("#26364F"));
                page.Header().PaddingBottom(4, Unit.Millimetre).Row(row => { row.RelativeItem().Text("RIFAS · " + comunidade).FontSize(9).SemiBold(); row.ConstantItem(150).AlignRight().Text("A4 · imprimir em 100%").FontSize(8); });
                page.Content().Column(col =>
                {
                    col.Spacing(4, Unit.Millimetre); foreach (var numero in folha)
                    {
                        ocupados.TryGetValue(numero, out var comprador); col.Item().Height(240f / porFolha, Unit.Millimetre).Border(0.5f).BorderColor("#8490A6").Row(row =>
                        {
                            row.RelativeItem(2).Padding(4, Unit.Millimetre).Column(c =>
                            {
                                c.Item().Row(h => { h.ConstantItem(9, Unit.Millimetre).Height(10, Unit.Millimetre).Image(_logo.Value).FitArea(); h.RelativeItem().PaddingLeft(3, Unit.Millimetre).AlignMiddle().Text("BILHETE DO PARTICIPANTE").FontSize(7).FontColor("#56647C"); h.ConstantItem(70).AlignRight().Text(numero.ToString("D4", CultureInfo.InvariantCulture)).FontSize(18).Bold().FontColor("#3063DA"); });
                                c.Item().PaddingTop(2).Text(rifa.Nome).FontSize(12).Bold();
                                c.Item().PaddingTop(2).Text("Sorteio: " + rifa.DataSorteio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + "   ·   " + rifa.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))).SemiBold();
                                c.Item().PaddingTop(3).Text("PRÊMIOS").FontSize(7).SemiBold();
                                c.Item().Text(string.Join("  ·  ", rifa.Premios.Split('\n').Select((p, i) => (i + 1).ToString(CultureInfo.InvariantCulture) + "º " + p))).FontSize(8);
                                c.Item().PaddingTop(3).Text("Comprador: " + (comprador?.Comprador ?? "________________________________")).FontSize(8);
                                c.Item().Text("Vendedor: " + (string.IsNullOrWhiteSpace(comprador?.Vendedor) ? "________________________________" : comprador.Vendedor)).FontSize(8);
                                c.Item().Text(comprador is null ? "Preencher no momento da venda." : comprador.Pago ? "Pagamento confirmado" : "Pagamento pendente").FontSize(7).FontColor("#56647C");
                            });
                            row.ConstantItem(0.5f).Background("#8490A6");
                            row.RelativeItem().Padding(3, Unit.Millimetre).Column(c =>
                            {
                                c.Item().Text("CANHOTO · ORGANIZAÇÃO").FontSize(7).SemiBold();
                                c.Item().PaddingVertical(2).Text(numero.ToString("D4", CultureInfo.InvariantCulture)).FontSize(18).Bold().FontColor("#3063DA");
                                c.Item().Text(rifa.Nome).FontSize(8).SemiBold();
                                c.Item().PaddingTop(3).Text("Nome: " + (comprador?.Comprador ?? "____________________")).FontSize(8);
                                c.Item().PaddingTop(3).Text("Telefone: " + (string.IsNullOrWhiteSpace(comprador?.Telefone) ? "___________________" : comprador.Telefone)).FontSize(8);
                                c.Item().PaddingTop(3).Text("Valor: " + rifa.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))).FontSize(8);
                                c.Item().Text("Recebido: ____/____/____").FontSize(8);
                            });
                        });
                    }
                });
                page.Footer().PaddingTop(3).Row(row => { row.RelativeItem().Text("Corte nas bordas dos bilhetes e destaque o canhoto na linha vertical.").FontSize(7); row.ConstantItem(80).AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); }); });
            });
        }).GeneratePdf();
    }
}
