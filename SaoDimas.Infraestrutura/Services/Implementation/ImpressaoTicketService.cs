using System.Globalization;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;

namespace SaoDimas.Infraestrutura.Services.Implementation;

/// <summary>
/// Tickets em folha A4 (tamanho real): 12 por folha, em 2 colunas × 6 linhas de 95 × 46 mm.
/// Cada ticket tem a parte do cliente e o canhoto de controle (com o mesmo número e o responsável da faixa),
/// separados por uma linha de destaque. Linhas de corte tracejadas entre os tickets.
/// </summary>
internal sealed class ImpressaoTicketService(IOptions<ConfiguracaoParoquia> opcoes, IHostEnvironment ambiente)
    : IImpressaoTicketService
{
    public const int Colunas = 2;
    public const int Linhas = 6;
    public const int TicketsPorFolha = Colunas * Linhas;

    // Geometria (mm). Margens deixam todo o conteúdo dentro da área imprimível (≥ 5 mm da borda).
    public const float MargemEsquerda = 10;
    public const float MargemTopo = 9;
    public const float LarguraTicket = 95;
    public const float AlturaTicket = 46;
    public const float LarguraCliente = 64;

    private const float LarguraFolha = 210;
    private const float AlturaFolha = 297;
    private const float Respiro = 3;

    private static readonly Color Verde = Color.FromHex("#17633A");
    private static readonly Color Roxo = Color.FromHex("#60358F");
    private static readonly Color Dourado = Color.FromHex("#C9952B");
    private static readonly Color Texto = Color.FromHex("#1B201D");
    private static readonly Color TextoSecundario = Color.FromHex("#56605A");
    private static readonly Color Linha = Color.FromHex("#8A948E");

    private static readonly CultureInfo PortuguesBrasil = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Ícone Lucide "scissors" (ISC), em unidades de 24 × 24.</summary>
    private const string Tesoura =
        "fill=\"none\" stroke=\"#56605A\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
        "<circle cx=\"6\" cy=\"6\" r=\"3\"/><path d=\"M8.12 8.12 12 12\"/><path d=\"M20 4 8.12 15.88\"/>" +
        "<circle cx=\"6\" cy=\"18\" r=\"3\"/><path d=\"M14.8 14.8 20 20\"/>";

    // Guias por quantidade de tickets na folha (1..12): a última folha só traz linhas onde há ticket.
    private static readonly string[] Guias = [.. Enumerable.Range(1, TicketsPorFolha).Select(GerarGuiasSvg)];

    // Carregada uma vez e reutilizada: a logo é embutida uma única vez no PDF.
    private readonly Lazy<Image> _logo = new(() =>
        Image.FromFile(Path.Combine(ambiente.ContentRootPath, opcoes.Value.CaminhoLogoImpressao)));

    public byte[] Gerar(ImpressaoTicketsDto impressao)
    {
        ArgumentNullException.ThrowIfNull(impressao);
        if (impressao.Tickets.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um ticket.", nameof(impressao));
        }

        var paroquia = opcoes.Value.Nome;
        var folhas = impressao.Tickets.Chunk(TicketsPorFolha).ToList();
        var primeiro = impressao.Tickets[0].Numero;
        var ultimo = impressao.Tickets[^1].Numero;

        return Document.Create(container =>
            {
                for (var indice = 0; indice < folhas.Count; indice++)
                {
                    var folha = folhas[indice];
                    var numeroFolha = indice + 1;
                    container.Page(pagina => ComporFolha(pagina, impressao, paroquia, folha, numeroFolha, folhas.Count));
                }
            })
            .WithMetadata(new DocumentMetadata
            {
                Title = $"Tickets — {impressao.Produto} — {impressao.Evento} ({Numero(primeiro)} a {Numero(ultimo)})",
                Author = paroquia,
                Subject = "Tickets para impressão em A4, tamanho real (100%)",
                Creator = paroquia,
                Producer = paroquia
            })
            .GeneratePdf();
    }

    public static string Numero(int numero) => numero.ToString("D6", CultureInfo.InvariantCulture);

    private void ComporFolha(
        PageDescriptor pagina, ImpressaoTicketsDto impressao, string paroquia, TicketImpressaoDto[] tickets, int folha, int totalFolhas)
    {
        pagina.Size(LarguraFolha, AlturaFolha, Unit.Millimetre);
        pagina.MarginLeft(MargemEsquerda, Unit.Millimetre);
        pagina.MarginRight(MargemEsquerda, Unit.Millimetre);
        pagina.MarginTop(MargemTopo, Unit.Millimetre);
        pagina.MarginBottom(6, Unit.Millimetre);
        pagina.PageColor(Colors.White);
        pagina.DefaultTextStyle(estilo => estilo.FontColor(Texto).LineHeight(1.1f));
        pagina.Background().Svg(Guias[tickets.Length - 1]);

        pagina.Content().Column(coluna =>
        {
            foreach (var linha in tickets.Chunk(Colunas))
            {
                coluna.Item().Height(AlturaTicket, Unit.Millimetre).Row(fileira =>
                {
                    foreach (var ticket in linha)
                    {
                        fileira.ConstantItem(LarguraTicket, Unit.Millimetre).Element(celula => ComporTicket(celula, impressao, paroquia, ticket));
                    }
                });
            }
        });

        pagina.Footer().AlignCenter().Text(texto =>
        {
            texto.DefaultTextStyle(estilo => estilo.FontSize(6.5f).FontColor(TextoSecundario));
            texto.Span($"{impressao.Evento}  •  {impressao.Produto}  •  Tickets {Numero(tickets[0].Numero)} a {Numero(tickets[^1].Numero)}  •  Folha {folha} de {totalFolhas}");
            texto.Span("  •  Imprimir em A4, tamanho real (100%)");
        });
    }

    private void ComporTicket(IContainer container, ImpressaoTicketsDto impressao, string paroquia, TicketImpressaoDto ticket)
    {
        container.Row(ticketRow =>
        {
            ticketRow.ConstantItem(LarguraCliente, Unit.Millimetre).Padding(Respiro, Unit.Millimetre)
                .Element(cliente => ComporParteDoCliente(cliente, impressao, paroquia, ticket));
            ticketRow.RelativeItem().PaddingVertical(Respiro, Unit.Millimetre).PaddingLeft(Respiro + 1, Unit.Millimetre).PaddingRight(Respiro, Unit.Millimetre)
                .Element(canhoto => ComporCanhoto(canhoto, impressao, ticket));
        });
    }

    private void ComporParteDoCliente(IContainer container, ImpressaoTicketsDto impressao, string paroquia, TicketImpressaoDto ticket)
    {
        container.Column(coluna =>
        {
            coluna.Item().Height(15, Unit.Millimetre).Row(cabecalho =>
            {
                cabecalho.ConstantItem(10.5f, Unit.Millimetre).AlignMiddle().Image(_logo.Value).FitArea();
                cabecalho.RelativeItem().PaddingLeft(2, Unit.Millimetre).AlignMiddle().Column(identidade =>
                {
                    identidade.Item().Text(paroquia.ToUpper(PortuguesBrasil)).FontSize(6.5f).Bold().FontColor(Verde).LetterSpacing(0.04f);
                    identidade.Item().Text(impressao.Comunidade).FontSize(5.5f).FontColor(TextoSecundario);
                    identidade.Item().PaddingTop(0.5f, Unit.Millimetre).Height(5.5f, Unit.Millimetre).ScaleToFit()
                        .Text(impressao.Evento).FontSize(6.5f).SemiBold();
                });
            });

            coluna.Item().PaddingTop(1, Unit.Millimetre).LineHorizontal(0.4f).LineColor(Dourado);

            coluna.Item().PaddingTop(1.5f, Unit.Millimetre).Height(6, Unit.Millimetre).AlignMiddle().ScaleToFit()
                .Text(impressao.Produto.ToUpper(PortuguesBrasil)).FontSize(13).Bold().FontColor(Roxo);

            coluna.Item().Row(rodape =>
            {
                rodape.RelativeItem().AlignBottom().Text(Moeda(impressao.Preco)).FontSize(12).Bold().FontColor(Verde);
                rodape.AutoItem().AlignBottom().Column(numero =>
                {
                    numero.Item().AlignRight().Text("TICKET Nº").FontSize(5.5f).SemiBold().FontColor(TextoSecundario).LetterSpacing(0.08f);
                    numero.Item().AlignRight().Text(Numero(ticket.Numero)).FontSize(11).Bold();
                });
            });

            coluna.Item().ExtendVertical().AlignBottom().Text("Apresente este ticket na retirada.").FontSize(5.5f).Italic().FontColor(TextoSecundario);
        });
    }

    private static void ComporCanhoto(IContainer container, ImpressaoTicketsDto impressao, TicketImpressaoDto ticket)
    {
        container.Column(coluna =>
        {
            coluna.Item().Text("CONTROLE").FontSize(5.5f).SemiBold().FontColor(Dourado).LetterSpacing(0.15f);
            coluna.Item().PaddingTop(0.5f, Unit.Millimetre).Height(4, Unit.Millimetre).ScaleToFit()
                .Text(impressao.Produto.ToUpper(PortuguesBrasil)).FontSize(7).Bold().FontColor(Roxo);
            coluna.Item().Text(Moeda(impressao.Preco)).FontSize(6.5f).FontColor(TextoSecundario);

            coluna.Item().PaddingTop(1.5f, Unit.Millimetre).Text("Nº").FontSize(5.5f).SemiBold().FontColor(TextoSecundario);
            coluna.Item().Text(Numero(ticket.Numero)).FontSize(10).Bold();

            coluna.Item().ExtendVertical().AlignBottom().Column(responsavel =>
            {
                responsavel.Item().Text("RESPONSÁVEL").FontSize(5).SemiBold().FontColor(TextoSecundario).LetterSpacing(0.08f);
                if (ticket.Responsavel is { } nome)
                {
                    responsavel.Item().Height(6, Unit.Millimetre).AlignMiddle().ScaleToFit().Text(nome).FontSize(7).SemiBold();
                }
                else
                {
                    // Faixa ainda sem responsável: espaço para escrever à mão.
                    responsavel.Item().Height(6, Unit.Millimetre).BorderBottom(0.5f).BorderColor(Linha);
                }
            });
        });
    }

    private static string Moeda(decimal valor) => valor.ToString("C2", PortuguesBrasil);

    /// <summary>
    /// Linhas de corte (tracejadas) ao redor dos tickets e linha de destaque (pontilhada, com tesoura) entre a
    /// parte do cliente e o canhoto, somente nas posições ocupadas. Coordenadas absolutas na folha, em milímetros.
    /// </summary>
    private static string GerarGuiasSvg(int quantidade)
    {
        var svg = new StringBuilder();
        string N(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);
        void Linha(float x1, float y1, float x2, float y2, string estilo) =>
            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" {estilo}/>");

        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(LarguraFolha)}mm\" height=\"{N(AlturaFolha)}mm\" viewBox=\"0 0 {N(LarguraFolha)} {N(AlturaFolha)}\">");

        const string corte = "stroke=\"#9AA39D\" stroke-width=\"0.2\" stroke-dasharray=\"1.6 1.2\"";
        const string destaque = "stroke=\"#56605A\" stroke-width=\"0.25\" stroke-dasharray=\"0.4 0.8\" stroke-linecap=\"round\"";

        for (var indice = 0; indice < quantidade; indice++)
        {
            var (linha, coluna) = Math.DivRem(indice, Colunas);
            var x = MargemEsquerda + (coluna * LarguraTicket);
            var y = MargemTopo + (linha * AlturaTicket);

            // Bordas compartilhadas com o vizinho já preenchido não são repetidas.
            if (linha == 0)
            {
                Linha(x, y, x + LarguraTicket, y, corte);
            }

            Linha(x, y, x, y + AlturaTicket, corte);
            Linha(x, y + AlturaTicket, x + LarguraTicket, y + AlturaTicket, corte);
            if (coluna == Colunas - 1 || indice == quantidade - 1)
            {
                Linha(x + LarguraTicket, y, x + LarguraTicket, y + AlturaTicket, corte);
            }

            var destaqueX = x + LarguraCliente;
            Linha(destaqueX, y + 5.5f, destaqueX, y + AlturaTicket - 1.5f, destaque);

            // Tesoura (ícone Lucide "scissors", ISC) sobre a linha de destaque.
            svg.Append(CultureInfo.InvariantCulture,
                $"<g transform=\"translate({N(destaqueX - 1.75f)} {N(y + 1.2f)}) scale(0.146)\" {Tesoura}</g>");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}
