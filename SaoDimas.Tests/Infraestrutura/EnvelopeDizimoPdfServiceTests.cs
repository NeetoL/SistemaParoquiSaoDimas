using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Infraestrutura.Services.Implementation;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Molde = SaoDimas.Infraestrutura.Services.Implementation.EnvelopeDizimoPdfService;

namespace SaoDimas.Tests.Infraestrutura;

/// <summary>
/// Gera o PDF real (QuestPDF) e o inspeciona com o PdfPig: dimensões físicas, dobras e posição do texto.
/// </summary>
public sealed class EnvelopeDizimoPdfServiceTests
{
    private const double PontosPorMm = 72 / 25.4;

    private static readonly DizimistaIdentificacaoDto Matriz = new(1, "000001", "João da Silva", "Paróquia São Dimas");
    private static readonly DizimistaIdentificacaoDto SantaTeresinha = new(2, "000002", "Maria da Conceição Gonçalves", "Capela Santa Teresinha");
    private static readonly DizimistaIdentificacaoDto SantoInacio = new(3, "000003", "José", "Capela Santo Inácio");
    private static readonly DizimistaIdentificacaoDto SantoExpedito = new(4, "000004", "Ana Lima", "Capela Santo Expedito");

    private static readonly DizimistaIdentificacaoDto NomeMuitoLongo = new(
        999_999, "999999",
        "Maria Aparecida Conceição de Nossa Senhora dos Remédios Gonçalves de Albuquerque Nascimento Teixeira",
        "Capela Santa Teresinha");

    private static EnvelopeDizimoPdfService CriarServico() => new(
        Options.Create(new ConfiguracaoParoquia
        {
            Nome = "Paróquia São Dimas",
            CaminhoLogoImpressao = "wwwroot/img/brasao-impressao.png"
        }),
        new AmbienteDeTeste { ContentRootPath = LocalizarRaizMvc() });

    [Fact]
    public void Folha_tem_exatamente_210_por_297_mm_em_retrato()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([Matriz]));
        var pagina = pdf.GetPage(1);

        Assert.Equal(210, pagina.Width / PontosPorMm, precision: 1);
        Assert.Equal(297, pagina.Height / PontosPorMm, precision: 1);
    }

    [Fact]
    public void Lote_imprime_endereco_da_igreja_correspondente_sem_misturar_modelos()
    {
        Dictionary<string, string> enderecos = new()
        {
            [Matriz.Comunidade] = "Rua Buique, 11 • Padre Miguel • Rio de Janeiro • RJ",
            [SantaTeresinha.Comunidade] = "Estrada Maravilha, 236 • Bangu • Rio de Janeiro • RJ",
            [SantoInacio.Comunidade] = "Rua Campo Largo, 255 • Padre Miguel • Rio de Janeiro • RJ",
            [SantoExpedito.Comunidade] = "Rua Limites, 908 • Realengo • Rio de Janeiro • RJ"
        };
        var servico = new EnvelopeDizimoPdfService(Options.Create(new ConfiguracaoParoquia
        {
            Nome = "Paróquia São Dimas", CaminhoLogoImpressao = "wwwroot/img/brasao-impressao.png",
            EnderecosComunidades = enderecos
        }), new AmbienteDeTeste { ContentRootPath = LocalizarRaizMvc() });
        DizimistaIdentificacaoDto[] dizimistas = [Matriz, SantaTeresinha, SantoInacio, SantoExpedito];
        using var pdf = PdfDocument.Open(servico.Gerar(dizimistas));
        for (var i = 0; i < dizimistas.Length; i++)
        {
            var pagina = pdf.GetPage(i + 1);
            var texto = SemEspacos(string.Concat(pagina.Letters.Select(letra => letra.Value)));
            Assert.Contains(SemEspacos(enderecos[dizimistas[i].Comunidade]), texto, StringComparison.Ordinal);
            foreach (var outro in enderecos.Where(item => item.Key != dizimistas[i].Comunidade))
                Assert.DoesNotContain(SemEspacos(outro.Value), texto, StringComparison.Ordinal);
            Assert.Empty(LetrasSobreDobras(pagina));
            Assert.Equal(3, pagina.GetImages().Count());
        }
    }

    [Fact]
    public void Lote_gera_uma_folha_por_dizimista_na_ordem_informada()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([Matriz, SantaTeresinha, SantoInacio, SantoExpedito]));

        Assert.Equal(4, pdf.NumberOfPages);
        Assert.Contains(SemEspacos(Matriz.Nome), SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
        Assert.Contains(SemEspacos("Capela Santa Teresinha"), SemEspacos(LetrasNaFaixa(pdf.GetPage(2), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
        Assert.Contains(SemEspacos("Capela Santo Inácio"), SemEspacos(LetrasNaFaixa(pdf.GetPage(3), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
        Assert.Contains(SemEspacos("Capela Santo Expedito"), SemEspacos(LetrasNaFaixa(pdf.GetPage(4), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Dizimistas))]
    public void Identificacao_completa_fica_no_verso_com_pix_e_frente_mantem_controle_mensal(DizimistaIdentificacaoDto dizimista)
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([dizimista]));
        var frente = TextoNaFaixa(pdf.GetPage(1), Molde.DobraFechamento, Molde.DobraBase);

        // O nome é comparado sem espaços: nomes longos são reduzidos e podem ocupar duas linhas.
        Assert.Contains(SemEspacos(dizimista.Nome), SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
        Assert.DoesNotContain(SemEspacos(dizimista.Nome), SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraFechamento, Molde.DobraBase)), StringComparison.Ordinal);
        foreach (var removido in new[] { "Forma de contribuição", "Dinheiro", "Transferência", "Outro" })
            Assert.DoesNotContain(removido, Texto(pdf.GetPage(1)), StringComparison.Ordinal);
        var verso = SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraBase, Molde.LimiteVisivelDoVerso));
        Assert.Contains(dizimista.Codigo, verso, StringComparison.Ordinal);
        Assert.Contains(SemEspacos(dizimista.Comunidade), verso, StringComparison.Ordinal);
        Assert.DoesNotContain(dizimista.Codigo, frente, StringComparison.Ordinal);
        Assert.DoesNotContain("CÓDIGO", frente, StringComparison.Ordinal);
        Assert.DoesNotContain("COMUNIDADE", frente, StringComparison.Ordinal);
        foreach (var esperado in new[] { dizimista.Comunidade.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pt-BR")), "MEU DÍZIMO", "MÊS", "DATA", "VALOR", "JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ", "13º", "2Cor 9,7" })
        {
            Assert.Contains(esperado, frente, StringComparison.Ordinal);
        }
    }

    public static TheoryData<DizimistaIdentificacaoDto> Dizimistas() => [Matriz, SantaTeresinha, SantoInacio, SantoExpedito, NomeMuitoLongo];

    [Fact]
    public void Nome_muito_longo_e_reduzido_e_nao_invade_outras_areas()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([NomeMuitoLongo]));

        Assert.Contains(SemEspacos(NomeMuitoLongo.Nome), SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraBase, Molde.LimiteVisivelDoVerso)), StringComparison.Ordinal);
        Assert.Empty(LetrasSobreDobras(pdf.GetPage(1)));
    }

    [Theory]
    [MemberData(nameof(Dizimistas))]
    public void Nenhum_texto_fica_sobre_dobras_nem_a_menos_de_5mm_da_borda(DizimistaIdentificacaoDto dizimista)
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([dizimista]));
        var pagina = pdf.GetPage(1);

        Assert.Empty(LetrasSobreDobras(pagina));
        Assert.All(pagina.Letters, letra =>
        {
            var caixa = Milimetros(letra, pagina);
            Assert.InRange(caixa.Esquerda, 5, Molde.LarguraFolha - 5);
            Assert.InRange(caixa.Direita, 5, Molde.LarguraFolha - 5);
            Assert.InRange(caixa.Topo, 5, Molde.AlturaFolha - 5);
            Assert.InRange(caixa.Base, 5, Molde.AlturaFolha - 5);
        });
    }

    [Fact]
    public void Nenhum_texto_fica_na_parte_do_verso_coberta_pela_aba_fechada()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([SantaTeresinha]));
        var pagina = pdf.GetPage(1);

        var cobertas = pagina.Letters
            .Select(letra => Milimetros(letra, pagina))
            .Where(caixa => caixa.Esquerda > Molde.AbaLateral && caixa.Direita < Molde.LarguraFolha - Molde.AbaLateral)
            .Where(caixa => caixa.Base > Molde.LimiteVisivelDoVerso);

        Assert.Empty(cobertas);
        // Texto girado 180°: verificado pela sequência de letras.
        var letras = SemEspacos(string.Concat(pagina.Letters.Select(letra => letra.Value)));
        Assert.Contains("ORAÇÃODENOSSASENHORADODÍZIMO", letras, StringComparison.Ordinal);
        Assert.True(letras.Contains("Dizimistanº000002", StringComparison.Ordinal), letras);
    }

    [Fact]
    public void Envelope_traz_campos_da_referencia_sem_imprimir_cpf()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([Matriz]));
        var texto = string.Concat(pdf.GetPage(1).Letters.Select(letra => letra.Value));

        Assert.DoesNotContain("CPF", texto, StringComparison.OrdinalIgnoreCase);
        foreach (var campo in new[] { "Telefone", "Endereço", "CEP", "Bairro", "Aniversário", "Pix" })
            Assert.Contains(campo, texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Telefone_cadastrado_e_oracao_completa_sao_impressos()
    {
        var conteudo = CriarServico().Gerar([Matriz with { Telefone = "(21) 99999-1234" }]);
        using var pdf = PdfDocument.Open(conteudo);
        var texto = SemEspacos(string.Concat(pdf.GetPage(1).Letters.Select(letra => letra.Value)));
        Assert.Contains("(21)99999-1234", texto, StringComparison.Ordinal);
        Assert.Contains("ÓMaria,gloriosamãedeDeus", texto, StringComparison.Ordinal);
        Assert.Contains("NossaSenhoradoDízimo,rogaipornós!", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Envelope_imprime_dados_cadastrados_e_aniversario_sem_ano()
    {
        using var pdf = PdfDocument.Open(CriarServico().Gerar([Matriz with
        {
            Endereco = "Rua das Flores, 10", Cep = "21775280", Bairro = "Padre Miguel",
            DataNascimento = new DateOnly(1987, 7, 12)
        }]));
        var texto = SemEspacos(LetrasNaFaixa(pdf.GetPage(1), Molde.DobraBase, Molde.LimiteVisivelDoVerso));
        foreach (var esperado in new[] { "RuadasFlores,10", "21775-280", "PadreMiguel", "12/07" })
            Assert.Contains(esperado, texto, StringComparison.Ordinal);
        Assert.DoesNotContain("1987", texto, StringComparison.Ordinal);
        Assert.Empty(LetrasSobreDobras(pdf.GetPage(1)));
    }

    [Fact]
    public void Logo_e_embutida_uma_unica_vez_mesmo_em_lote()
    {
        var umaFolha = CriarServico().Gerar([Matriz]);
        var quatroFolhas = CriarServico().Gerar([Matriz, SantaTeresinha, SantoInacio, SantoExpedito]);

        using var pdf = PdfDocument.Open(umaFolha);
        // Brasão, símbolo do dízimo e QR Code original do Pix.
        Assert.Equal(3, pdf.GetPage(1).GetImages().Count());
        // Folhas extras acrescentam somente texto e vetores, não novas cópias da imagem.
        Assert.InRange(quatroFolhas.Length - umaFolha.Length, 0, 3 * 40 * 1024);
    }

    private static List<string> LetrasSobreDobras(Page pagina)
    {
        const double folga = 1.5; // mm
        return pagina.Letters
            .Select(letra => (letra.Value, Caixa: Milimetros(letra, pagina)))
            .Where(item =>
                Cruza(item.Caixa.Topo, item.Caixa.Base, Molde.DobraFechamento, folga)
                || Cruza(item.Caixa.Topo, item.Caixa.Base, Molde.DobraBase, folga)
                || Cruza(item.Caixa.Esquerda, item.Caixa.Direita, Molde.AbaLateral, folga)
                || Cruza(item.Caixa.Esquerda, item.Caixa.Direita, Molde.LarguraFolha - Molde.AbaLateral, folga))
            .Select(item => item.Value)
            .ToList();
    }

    private static bool Cruza(double inicio, double fim, double dobra, double folga) =>
        inicio < dobra + folga && fim > dobra - folga;

    /// <summary>
    /// Caixa da letra em mm com origem no canto superior esquerdo (como no molde).
    /// </summary>
    private static (double Esquerda, double Direita, double Topo, double Base) Milimetros(Letter letra, Page pagina)
    {
        var caixa = letra.BoundingBox;
        return (
            caixa.Left / PontosPorMm,
            caixa.Right / PontosPorMm,
            (pagina.Height - caixa.Top) / PontosPorMm,
            (pagina.Height - caixa.Bottom) / PontosPorMm);
    }

    private static string TextoNaFaixa(Page pagina, double yInicioMm, double yFimMm) =>
        string.Join(' ', pagina.GetWords()
            .Where(palavra =>
            {
                var topo = (pagina.Height - palavra.BoundingBox.Top) / PontosPorMm;
                return topo >= yInicioMm && topo <= yFimMm;
            })
            .Select(palavra => palavra.Text));

    private static string LetrasNaFaixa(Page pagina, double yInicioMm, double yFimMm) =>
        string.Concat(pagina.Letters
            .Where(letra =>
            {
                var topo = (pagina.Height - letra.BoundingBox.Top) / PontosPorMm;
                return topo >= yInicioMm && topo <= yFimMm;
            })
            .Select(letra => letra.Value));

    private static string SemEspacos(string texto) => string.Concat(texto.Where(c => !char.IsWhiteSpace(c)));

    private static string Texto(Page pagina) => string.Join(' ', pagina.GetWords().Select(palavra => palavra.Text));

    private static string LocalizarRaizMvc()
    {
        for (var diretorio = new DirectoryInfo(AppContext.BaseDirectory); diretorio is not null; diretorio = diretorio.Parent)
        {
            var candidato = Path.Combine(diretorio.FullName, "SaoDimas.MVC");
            if (File.Exists(Path.Combine(candidato, "wwwroot", "img", "brasao-impressao.png")))
            {
                return candidato;
            }
        }

        throw new InvalidOperationException("Logo de impressão não encontrada. Compile o SaoDimas.MVC (dotnet build).");
    }
}
