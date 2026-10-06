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
/// Molde do Envelope de Dízimo: A4 inteiro, sem cortes, com frente e verso iguais (190 × 118,5 mm).
/// <para>
/// Geometria (mm, origem no canto superior esquerdo da folha):
/// abas laterais de 10 mm (x 0–10 e 200–210); fechamento y 0–60; frente y 60–178,5; verso y 178,5–297.
/// A borda inferior encosta na dobra superior ao fechar a base. As abas laterais do verso recebem cola.
/// </para>
/// <para>
/// O verso e a aba ficam atrás do envelope montado e por isso são impressos girados 180°.
/// Instruções e a régua de conferência ficam nas abas laterais, que somem para dentro após a montagem.
/// </para>
/// </summary>
internal sealed class EnvelopeDizimoPdfService(IOptions<ConfiguracaoParoquia> opcoes, IHostEnvironment ambiente)
    : IEnvelopeDizimoPdfService
{
    // Folha e dobras (mm).
    public const float LarguraFolha = 210;
    public const float AlturaFolha = 297;
    public const float AbaLateral = 10;
    public const float DobraFechamento = 60;
    // Dois painéis exatamente iguais: dobrar a borda inferior até a linha superior basta para alinhar.
    public const float AlturaEnvelope = (AlturaFolha - DobraFechamento) / 2;
    public const float DobraBase = DobraFechamento + AlturaEnvelope;

    /// <summary>
    /// Com a aba fechada (60 mm sobre o verso), só a faixa do verso entre a dobra da base e esta linha fica visível.
    /// A tabela de contribuições fica depois desta linha, coberta pela aba; o Pix permanece visível.
    /// </summary>
    public const float LimiteVisivelDoVerso = DobraBase + (DobraBase - DobraFechamento) - DobraFechamento;

    /// <summary>Distância mínima entre qualquer texto e uma dobra ou borda da folha.</summary>
    public const float AreaSegura = 6;

    private const float DobraDireita = LarguraFolha - AbaLateral;

    /// <summary>Altura de cada linha de preenchimento à mão (mm).</summary>
    private const float AlturaLinhaEscrita = 9;

    // Régua de conferência de 50 mm na aba lateral direita da frente.
    private const float XRegua = DobraDireita + 0.8f;
    private const float YRegua = DobraFechamento + 3;

    private static readonly Color Verde = Color.FromHex("#17633A");
    private static readonly Color Roxo = Color.FromHex("#60358F");
    private static readonly Color Dourado = Color.FromHex("#C9952B");
    private static readonly Color Texto = Color.FromHex("#1B201D");
    private static readonly Color TextoSecundario = Color.FromHex("#56605A");
    private static readonly Color Linha = Color.FromHex("#8A948E");

    private static readonly string[] TitulosControleMensal = ["MÊS", "DATA", "VALOR"];
    private static readonly CultureInfo PortuguesBrasil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string Guias = GerarGuiasSvg();

    // Carregada uma vez e reutilizada: em lote, a logo é embutida uma única vez no PDF.
    private readonly Lazy<Image> _logo = new(() =>
        Image.FromFile(Path.Combine(ambiente.ContentRootPath, opcoes.Value.CaminhoLogoImpressao)));

    private readonly Lazy<Image> _qrPix = new(() =>
        Image.FromFile(Path.Combine(ambiente.ContentRootPath, "wwwroot/img/pix-qrcode.png")));

    private readonly Lazy<Image> _simboloDizimo = new(() =>
        Image.FromFile(Path.Combine(ambiente.ContentRootPath, "wwwroot/img/dizimo-expressao-fe.jpeg")));

    public byte[] Gerar(IReadOnlyList<DizimistaIdentificacaoDto> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);
        if (envelopes.Count == 0)
        {
            throw new ArgumentException("Informe ao menos um envelope.", nameof(envelopes));
        }

        var paroquia = opcoes.Value;

        return Document.Create(container =>
            {
                foreach (var envelope in envelopes)
                {
                    container.Page(pagina => ComporFolha(pagina, envelope, paroquia));
                }
            })
            .WithMetadata(new DocumentMetadata
            {
                Title = envelopes.Count == 1 ? $"Envelope de Dízimo — {envelopes[0].Nome}" : $"Envelopes de Dízimo ({envelopes.Count})",
                Author = paroquia.Nome,
                Subject = "Envelope de Dízimo — imprimir em A4, tamanho real (100%)",
                Creator = paroquia.Nome,
                Producer = paroquia.Nome
            })
            .GeneratePdf();
    }

    private void ComporFolha(PageDescriptor pagina, DizimistaIdentificacaoDto envelope, ConfiguracaoParoquia paroquia)
    {
        // Tamanho exato em milímetros (não o A4 arredondado em pontos): impressão em escala 100% previsível.
        pagina.Size(LarguraFolha, AlturaFolha, Unit.Millimetre);
        pagina.Margin(0);
        pagina.PageColor(Colors.White);
        pagina.DefaultTextStyle(estilo => estilo.FontColor(Texto).LineHeight(1.2f));

        pagina.Background().Svg(Guias);

        pagina.Content().Layers(camadas =>
        {
            camadas.PrimaryLayer().Extend();

            Area(camadas.Layer(), AbaLateral + AreaSegura, DobraFechamento + AreaSegura, DobraDireita - AreaSegura, DobraBase - AreaSegura)
                .Element(frente => ComporFrente(frente, envelope, paroquia));

            Area(camadas.Layer(), AbaLateral + AreaSegura, DobraBase + AreaSegura, DobraDireita - AreaSegura, AlturaFolha - AreaSegura)
                .RotateLayoutClockwise().RotateLayoutClockwise()
                .Element(verso => ComporVerso(verso, envelope, paroquia));

            Area(camadas.Layer(), AbaLateral + AreaSegura, 8, DobraDireita - AreaSegura, DobraFechamento - AreaSegura)
                .RotateLayoutClockwise().RotateLayoutClockwise()
                .Element(aba => ComporAbaDeFechamento(aba, envelope, paroquia));

            // Instruções de montagem nas abas laterais (ficam escondidas dentro do envelope montado).
            const float xEsquerda = 5.4f, xDireita = DobraDireita + 1;
            Instrucao(camadas.Layer(), xEsquerda, DobraFechamento + 4, DobraBase - 4, "1  Dobre as abas laterais para trás, nos tracejados.", girarParaEsquerda: true);
            Instrucao(camadas.Layer(), xEsquerda, 7, DobraFechamento - 3, "2  Passe cola nas áreas hachuradas.", girarParaEsquerda: true);
            Instrucao(camadas.Layer(), xDireita, 7, DobraFechamento - 3, "3  Encoste a borda inferior nesta dobra e pressione.", girarParaEsquerda: false);
            Instrucao(camadas.Layer(), xDireita, DobraFechamento + 57, DobraBase - 4, "4  Coloque a contribuição e feche a aba superior.", girarParaEsquerda: false);
            Instrucao(camadas.Layer(), XRegua + 1.8f, YRegua, YRegua + 50, "50 mm — confira a escala 100%", girarParaEsquerda: false, largura: 2.4f, tamanho: 5);
        });
    }

    private void ComporFrente(IContainer container, DizimistaIdentificacaoDto envelope, ConfiguracaoParoquia paroquia)
    {
        container.Column(coluna =>
        {
            coluna.Item().Height(24, Unit.Millimetre).Row(cabecalho =>
            {
                cabecalho.ConstantItem(17, Unit.Millimetre).AlignMiddle().Image(_logo.Value).FitArea();
                cabecalho.RelativeItem().PaddingLeft(4, Unit.Millimetre).AlignMiddle().Column(titulo =>
                {
                    titulo.Item().Text(envelope.Comunidade.ToUpper(PortuguesBrasil)).FontSize(11.5f).Bold().FontColor(Verde).LetterSpacing(0.06f);
                    titulo.Item().Text("MEU DÍZIMO").FontSize(22).Bold().FontColor(Roxo).LetterSpacing(0.08f);
                    if (!string.IsNullOrWhiteSpace(paroquia.Subtitulo))
                    {
                        titulo.Item().Text(paroquia.Subtitulo).FontSize(7.5f).FontColor(TextoSecundario);
                    }
                });
                cabecalho.ConstantItem(25, Unit.Millimetre).PaddingLeft(3, Unit.Millimetre)
                    .AlignMiddle().Height(22, Unit.Millimetre).Image(_simboloDizimo.Value).FitArea();
            });

            coluna.Item().PaddingVertical(2.5f, Unit.Millimetre).LineHorizontal(0.6f).LineColor(Dourado);

            coluna.Item().PaddingTop(3, Unit.Millimetre).BorderLeft(1.5f).BorderColor(Verde)
                .Background("#F7F8F6").Padding(4, Unit.Millimetre).Column(dados =>
                {
                    dados.Spacing(3, Unit.Millimetre);
                    Identificacao(dados.Item(), "Dizimista", envelope.Nome);
                    dados.Item().Row(linha =>
                    {
                        linha.Spacing(5, Unit.Millimetre);
                        Identificacao(linha.RelativeItem(), "Código", envelope.Codigo);
                        Identificacao(linha.RelativeItem(3), "Comunidade", envelope.Comunidade);
                    });
                    dados.Item().Row(linha =>
                    {
                        linha.Spacing(5, Unit.Millimetre);
                        DadoCadastrado(linha.RelativeItem(3), "Endereço", envelope.Endereco);
                        DadoCadastrado(linha.RelativeItem(), "Aniversário (dia/mês)", envelope.DataNascimento?.ToString("dd/MM", PortuguesBrasil));
                    });
                    dados.Item().Row(linha =>
                    {
                        linha.Spacing(5, Unit.Millimetre);
                        DadoCadastrado(linha.RelativeItem(), "CEP", envelope.Cep is { Length: 8 } cep ? $"{cep[..5]}-{cep[5..]}" : envelope.Cep);
                        DadoCadastrado(linha.RelativeItem(2), "Bairro", envelope.Bairro);
                        DadoCadastrado(linha.RelativeItem(2), "Telefone", envelope.Telefone);
                    });
                });

            coluna.Item().ExtendVertical().AlignBottom().AlignCenter().PaddingHorizontal(10, Unit.Millimetre).Text(texto =>
            {
                texto.AlignCenter();
                texto.DefaultTextStyle(estilo => estilo.FontSize(7.5f).Italic().FontColor(TextoSecundario));
                texto.Span("“Cada um dê conforme determinou em seu coração, não com pesar ou por obrigação, pois Deus ama quem dá com alegria.” ");
                texto.Span("2Cor 9,7").SemiBold().FontColor(Verde);
            });
        });
    }

    private static void ControleMensal(IContainer container)
    {
        string[] meses = ["JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ"];
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(colunas =>
            {
                for (var grupo = 0; grupo < 2; grupo++)
                {
                    colunas.RelativeColumn(0.8f);
                    colunas.RelativeColumn();
                    colunas.RelativeColumn(1.3f);
                }
            });
            for (var grupo = 0; grupo < 2; grupo++)
                foreach (var titulo in TitulosControleMensal)
                    tabela.Cell().Border(0.4f).BorderColor("#B9C8BE").Background("#EEF2EE")
                        .Height(5, Unit.Millimetre).AlignMiddle().AlignCenter()
                        .Text(titulo).FontSize(7.5f).SemiBold().FontColor(TextoSecundario);
            for (var linha = 0; linha < 7; linha++)
                for (var grupo = 0; grupo < 2; grupo++)
                    foreach (var valor in new[] { linha < 6 ? meses[linha + grupo * 6] : grupo == 1 ? "13º" : "", "", "" })
                        tabela.Cell().Border(0.4f).BorderColor("#B9C8BE")
                            .Background(valor.Length > 0 ? "#F3F7F4" : "#FFFFFF")
                            .Height(5.5f, Unit.Millimetre).AlignMiddle().AlignCenter()
                            .Text(valor).FontSize(8).SemiBold().FontColor(valor.Length > 0 ? Verde : Texto);
        });
    }

    private static void DadoCadastrado(IContainer container, string rotulo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) CampoComLinha(container, rotulo, null);
        else Identificacao(container, rotulo, valor);
    }

    private static void Identificacao(IContainer container, string rotulo, string valor)
    {
        container.Column(coluna =>
        {
            coluna.Item().Text(rotulo.ToUpper(PortuguesBrasil)).FontSize(6.5f).SemiBold().FontColor(TextoSecundario).LetterSpacing(0.08f);
            // Nomes longos são reduzidos para caber, nunca cortados.
            coluna.Item().Height(6.5f, Unit.Millimetre).AlignMiddle().ScaleToFit().Text(valor).FontSize(11.5f).SemiBold();
        });
    }

    /// <summary>
    /// Campo para preenchimento à mão: rótulo e linha com altura confortável para escrita com caneta.
    /// </summary>
    private static void CampoComLinha(IContainer container, string rotulo, string? prefixo, int linhas = 1)
    {
        container.Column(coluna =>
        {
            coluna.Item().Text(rotulo).FontSize(8).SemiBold().FontColor(TextoSecundario);
            for (var i = 0; i < linhas; i++)
            {
                var primeira = i == 0;
                coluna.Item().Height(AlturaLinhaEscrita, Unit.Millimetre).Row(linha =>
                {
                    if (prefixo is not null && primeira)
                    {
                        linha.AutoItem().AlignBottom().PaddingRight(1.5f, Unit.Millimetre).Text(prefixo).FontSize(11).SemiBold();
                    }

                    linha.RelativeItem().BorderBottom(0.6f).BorderColor(Linha);
                });
            }
        });
    }

    /// <summary>
    /// Verso: controle mensal sob a aba de fechamento e pagamento Pix na faixa visível.
    /// </summary>
    private void ComporVerso(IContainer container, DizimistaIdentificacaoDto envelope, ConfiguracaoParoquia paroquia)
    {
        container.Column(coluna =>
        {
            // Após a dobra, esta faixa fica integralmente sob a aba de fechamento.
            coluna.Item().Height(4, Unit.Millimetre).Text("CONTROLE DE CONTRIBUIÇÕES")
                .FontSize(7).SemiBold().FontColor(TextoSecundario).LetterSpacing(0.06f);
            coluna.Item().Element(ControleMensal);
            coluna.Item().ExtendVertical().AlignBottom().Row(verso =>
            {
                verso.Spacing(8, Unit.Millimetre);
                verso.RelativeItem().AlignMiddle().Column(pagamento =>
                {
                    pagamento.Item().Text("Pagamento via Pix").FontSize(11).SemiBold().FontColor(Roxo);
                    if (!string.IsNullOrWhiteSpace(paroquia.Telefone))
                        pagamento.Item().PaddingTop(2, Unit.Millimetre).Text($"Enviar comprovante para {paroquia.Telefone}").FontSize(8).FontColor(TextoSecundario);
                    else
                        pagamento.Item().PaddingTop(2, Unit.Millimetre).Text("Consulte a secretaria paroquial para os dados de pagamento e envio do comprovante.").FontSize(8).FontColor(TextoSecundario);
                });
                verso.ConstantItem(28, Unit.Millimetre).Column(pix =>
                {
                    pix.Item().Width(28, Unit.Millimetre).Height(28, Unit.Millimetre).Image(_qrPix.Value).FitArea();
                    pix.Item().PaddingTop(1, Unit.Millimetre).AlignCenter().Text("PIX").FontSize(8).SemiBold().FontColor(Roxo);
                });
            });
        });
    }
    private static void ComporAbaDeFechamento(IContainer container, DizimistaIdentificacaoDto envelope, ConfiguracaoParoquia paroquia)
    {
        container.Column(coluna =>
        {
            coluna.Item().Text("ORAÇÃO DE NOSSA SENHORA DO DÍZIMO").FontSize(10).Bold().FontColor(Roxo).AlignCenter();
            coluna.Item().PaddingTop(2, Unit.Millimetre).Text(
                "Ó Maria, gloriosa mãe de Deus, ergue-se a vós nossa oração. Que a sua fidelidade a Deus e o seu projeto de salvação, nos conceda a graça de sermos fiéis no dízimo, para colaborar com Deus, com a Igreja e com os pobres, e construir uma unidade concreta para suportar as adversidades e dificuldades da vida. Queremos que a Palavra de Deus, explicada ao povo, o faça cada vez mais devoto de Nossa Senhora e comprometido com o dízimo e a oferta. Amém.")
                .FontSize(8.5f).LineHeight(1.25f).FontColor(TextoSecundario).Justify();
            coluna.Item().PaddingTop(2, Unit.Millimetre).Text("Nossa Senhora do Dízimo, rogai por nós!").FontSize(9).SemiBold().FontColor(Verde).AlignCenter();
            var endereco = paroquia.EnderecosComunidades.FirstOrDefault(item =>
                string.Equals(item.Key, envelope.Comunidade, StringComparison.OrdinalIgnoreCase)).Value;
            if (string.IsNullOrWhiteSpace(endereco) && string.Equals(envelope.Comunidade, paroquia.Nome, StringComparison.OrdinalIgnoreCase))
                endereco = paroquia.Endereco;
            coluna.Item().PaddingTop(1, Unit.Millimetre).Text(envelope.Comunidade).FontSize(7).SemiBold().FontColor(Verde).AlignCenter();
            if (!string.IsNullOrWhiteSpace(endereco))
                coluna.Item().Text(endereco).FontSize(7).FontColor(TextoSecundario).AlignCenter();
            coluna.Item().ExtendVertical().AlignBottom().AlignCenter()
                .Text("A4 • Tamanho real (100%) • Sem cortes • Feche com cola ou fita adesiva")
                .FontSize(6.5f).FontColor(TextoSecundario);
        });
    }

    private static void Instrucao(
        IContainer camada, float x, float yInicio, float yFim, string texto, bool girarParaEsquerda, float largura = 3.6f, float tamanho = 6)
    {
        var area = Area(camada, x, yInicio, x + largura, yFim);
        (girarParaEsquerda ? area.RotateLayoutCounterclockwise() : area.RotateLayoutClockwise())
            .AlignMiddle().AlignCenter().ScaleToFit()
            .Text(texto).FontSize(tamanho).FontColor(TextoSecundario);
    }

    private static IContainer Area(IContainer camada, float x1, float y1, float x2, float y2) =>
        camada
            .PaddingLeft(x1, Unit.Millimetre)
            .PaddingTop(y1, Unit.Millimetre)
            .Width(x2 - x1, Unit.Millimetre)
            .Height(y2 - y1, Unit.Millimetre);

    /// <summary>
    /// Guias vetoriais do molde em escala real (unidades em mm): dobras tracejadas, áreas de cola,
    /// moldura da frente e régua de conferência de 50 mm.
    /// </summary>
    private static string GerarGuiasSvg()
    {
        var svg = new StringBuilder();
        string N(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);
        void Linha(float x1, float y1, float x2, float y2, string estilo) =>
            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" {estilo}/>");

        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(LarguraFolha)}mm\" height=\"{N(AlturaFolha)}mm\" viewBox=\"0 0 {N(LarguraFolha)} {N(AlturaFolha)}\">");

        // Dobras (tracejado).
        const string tracejado = "stroke=\"#7D8781\" stroke-width=\"0.25\" stroke-dasharray=\"2 1.5\"";
        // Não depender de impressão sem margens: todos os traços úteis ficam a 5 mm das bordas.
        Linha(AbaLateral, 5, AbaLateral, AlturaFolha - 5, tracejado);
        Linha(DobraDireita, 5, DobraDireita, AlturaFolha - 5, tracejado);
        Linha(5, DobraFechamento, LarguraFolha - 5, DobraFechamento, tracejado);
        Linha(5, DobraBase, LarguraFolha - 5, DobraBase, tracejado);
        // Marcas de encaixe reforçam a linha onde deve chegar a borda inferior.
        const string encaixe = "stroke=\"#17633A\" stroke-width=\"0.65\"";
        Linha(5, DobraFechamento, AbaLateral + 3, DobraFechamento, encaixe);
        Linha(DobraDireita - 3, DobraFechamento, LarguraFolha - 5, DobraFechamento, encaixe);

        // Áreas de cola: abas laterais do verso, hachuradas (dentro da área imprimível, a partir de 5 mm da borda).
        const string hachura = "stroke=\"#B5BDB7\" stroke-width=\"0.3\"";
        foreach (var (x1, x2) in new[] { (5f, 9.3f), (DobraDireita + 0.7f, LarguraFolha - 5f) })
        {
            for (var y = DobraBase + 3; y + (x2 - x1) <= AlturaFolha - 5; y += 2.5f)
            {
                Linha(x1, y, x2, y + (x2 - x1), hachura);
            }
        }

        // Moldura discreta da frente (dourado).
        svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{N(AbaLateral + 3)}\" y=\"{N(DobraFechamento + 3)}\" width=\"{N(DobraDireita - AbaLateral - 6)}\" height=\"{N(DobraBase - DobraFechamento - 6)}\" rx=\"2\" fill=\"none\" stroke=\"#C9952B\" stroke-width=\"0.35\"/>");

        // Régua de conferência de 50 mm na aba lateral direita da frente (some dentro do envelope).
        const string regua = "stroke=\"#56605A\" stroke-width=\"0.25\"";
        Linha(XRegua, YRegua, XRegua, YRegua + 50, regua);
        for (var marca = 0; marca <= 50; marca += 10)
        {
            Linha(XRegua, YRegua + marca, XRegua + (marca % 50 == 0 ? 1.6f : 0.9f), YRegua + marca, regua);
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}
