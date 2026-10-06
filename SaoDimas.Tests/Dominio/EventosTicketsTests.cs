using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Tests.Dominio;

public sealed class EventosTicketsTests
{
    private static readonly DateTimeOffset Agora = RelogioFixo.Padrao;

    private static Evento NovoEvento()
    {
        var evento = Evento.Criar(
            "Festa de Santa Teresinha 2026", null, Criar.Comunidade(2, nome: "Santa Teresinha"),
            new DateOnly(2026, 9, 19), new DateOnly(2026, 9, 27), null, Agora).Valor;
        evento.AdicionarProduto("Feijoada", null, 25.00m, Agora);
        return evento;
    }

    private static (Evento Evento, LoteTicket Lote) LoteDeCem()
    {
        var evento = NovoEvento();
        var lote = LoteTicket.Gerar(evento, evento.Produtos[0].Id, 100, Agora).Valor;
        return (evento, lote);
    }

    private static LoteTicket ComExemploObrigatorio(Evento evento, LoteTicket lote)
    {
        // Ids atribuídos como a persistência faria.
        Criar.ComId(lote.Distribuir(evento, 1, 20, "João da Silva", Agora).Valor, 1);
        Criar.ComId(lote.Distribuir(evento, 21, 50, "Maria Souza", Agora).Valor, 2);
        Criar.ComId(lote.Distribuir(evento, 51, 75, "Carlos Santos", Agora).Valor, 3);
        return lote;
    }

    [Fact]
    public void Lote_de_100_tickets_tem_quantidade_e_valor_potencial_corretos()
    {
        var (_, lote) = LoteDeCem();

        Assert.Equal(1, lote.NumeroInicial);
        Assert.Equal(100, lote.NumeroFinal);
        Assert.Equal(100, lote.Quantidade);
        Assert.Equal(25.00m, lote.PrecoUnitario);
        Assert.Equal(2_500.00m, lote.ValorPotencial);
    }

    [Fact]
    public void Exemplo_obrigatorio_75_distribuidos_25_disponiveis_e_sobreposicao_rejeitada()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);

        Assert.Equal(75, lote.QuantidadeDistribuida);
        Assert.Equal(25, lote.QuantidadeDisponivel);
        Assert.Equal(3, lote.TotalResponsaveis);
        Assert.Equal("076–100", Assert.Single(lote.Lacunas()).ToString());

        var jose = lote.Distribuir(evento, 15, 30, "José", Agora);

        Assert.False(jose.Sucesso);
        Assert.Contains("se sobrepõe à faixa 001–020 (João da Silva)", Assert.Single(jose.Erros).Mensagem, StringComparison.Ordinal);
        Assert.Equal(3, lote.Distribuicoes.Count);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(90, 110)]
    [InlineData(101, 120)]
    public void Faixa_fora_do_lote_e_rejeitada(int inicial, int final)
    {
        var (evento, lote) = LoteDeCem();

        Assert.False(lote.Distribuir(evento, inicial, final, "Maria", Agora).Sucesso);
        Assert.Empty(lote.Distribuicoes);
    }

    [Fact]
    public void Numero_inicial_maior_que_o_final_e_rejeitado()
    {
        var (evento, lote) = LoteDeCem();

        var resultado = lote.Distribuir(evento, 30, 10, "Maria", Agora);

        Assert.Equal("NumeroFinal", Assert.Single(resultado.Erros).Campo);
    }

    [Fact]
    public void Responsavel_e_opcional_e_faixa_sem_responsavel_nao_conta_como_distribuida()
    {
        var (evento, lote) = LoteDeCem();

        Assert.True(lote.Distribuir(evento, 1, 20, "   ", Agora).Sucesso);

        Assert.Null(lote.Distribuicoes[0].Responsavel);
        Assert.Equal(0, lote.QuantidadeDistribuida);
        Assert.Equal(20, lote.QuantidadeEmFaixasSemResponsavel);
        Assert.Equal(80, lote.QuantidadeLivre);
        Assert.Equal(100, lote.QuantidadeDisponivel);
    }

    [Fact]
    public void Mesmo_responsavel_pode_ter_varias_faixas()
    {
        var (evento, lote) = LoteDeCem();
        lote.Distribuir(evento, 1, 20, "João da Silva", Agora);
        lote.Distribuir(evento, 21, 40, "Maria Souza", Agora);
        lote.Distribuir(evento, 41, 60, "joão da silva", Agora);

        Assert.Equal(2, lote.TotalResponsaveis);
        Assert.Equal(40, lote.Distribuicoes.Where(d => d.Responsavel!.MesmaPessoa(lote.Distribuicoes[0].Responsavel)).Sum(d => d.Quantidade));
    }

    [Fact]
    public void Identifica_varias_lacunas_disponiveis()
    {
        var (evento, lote) = LoteDeCem();
        lote.Distribuir(evento, 1, 20, "João", Agora);
        lote.Distribuir(evento, 31, 40, "Maria", Agora);
        lote.Distribuir(evento, 51, 100, "Carlos", Agora);

        Assert.Equal(["021–030", "041–050"], lote.Lacunas().Select(lacuna => lacuna.ToString()));
    }

    [Theory]
    [InlineData(15, "João da Silva")]
    [InlineData(35, "Maria Souza")]
    [InlineData(60, "Carlos Santos")]
    [InlineData(80, null)]
    public void Responsavel_do_ticket_e_descoberto_pela_faixa(int numero, string? esperado)
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);

        Assert.Equal(esperado, lote.ResponsavelDoTicket(numero)?.Nome);
    }

    [Fact]
    public void Edicao_valida_sobreposicao_ignorando_a_propria_faixa()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);
        var maria = lote.Distribuicoes[1];

        Assert.True(lote.AlterarDistribuicao(evento, maria.Id, 21, 55, "Maria Souza", Agora).Sucesso is false);
        Assert.True(lote.AlterarDistribuicao(evento, maria.Id, 21, 45, "Maria S. Souza", Agora).Sucesso);
        Assert.Equal("Maria S. Souza", lote.ResponsavelDoTicket(45)!.Nome);
        Assert.Null(lote.ResponsavelDoTicket(48));
    }

    [Fact]
    public void Faixa_pode_ser_removida_antes_da_prestacao_mas_nao_depois()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);
        var (joao, maria) = (lote.Distribuicoes[0], lote.Distribuicoes[1]);

        Assert.True(lote.RegistrarPrestacao(evento, joao.Id, 18, 2, 450m, null, Agora).Sucesso);

        Assert.False(lote.RemoverDistribuicao(evento, joao.Id, Agora).Sucesso);
        Assert.False(lote.AlterarDistribuicao(evento, joao.Id, 1, 10, "João", Agora).Sucesso);
        Assert.True(lote.RemoverDistribuicao(evento, maria.Id, Agora).Sucesso);
        Assert.Equal(2, lote.Distribuicoes.Count);
    }

    [Fact]
    public void Prestacao_calcula_valor_esperado_e_diferenca_zero()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);
        var joao = lote.Distribuicoes[0];

        Assert.True(lote.RegistrarPrestacao(evento, joao.Id, 18, 2, 450.00m, null, Agora).Sucesso);

        var prestacao = joao.Prestacao!;
        Assert.Equal(20, prestacao.Recebidos);
        Assert.Equal(450.00m, prestacao.ValorEsperado);
        Assert.Equal(0m, prestacao.Diferenca);
        Assert.Equal(StatusDistribuicao.PrestacaoRegistrada, joao.Status);
    }

    [Fact]
    public void Diferenca_financeira_exige_justificativa_e_fica_registrada()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);
        var joao = lote.Distribuicoes[0];

        var semJustificativa = lote.RegistrarPrestacao(evento, joao.Id, 18, 2, 400.00m, " ", Agora);
        var comJustificativa = lote.RegistrarPrestacao(evento, joao.Id, 18, 2, 400.00m, "Dois tickets pagos depois.", Agora);

        Assert.Equal("Justificativa", Assert.Single(semJustificativa.Erros).Campo);
        Assert.True(comJustificativa.Sucesso);
        Assert.Equal(-50.00m, joao.Prestacao!.Diferenca);
    }

    [Fact]
    public void Vendidos_mais_devolvidos_deve_fechar_com_os_recebidos()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);

        var resultado = lote.RegistrarPrestacao(evento, lote.Distribuicoes[0].Id, 18, 1, 450m, null, Agora);

        Assert.Equal("Devolvidos", Assert.Single(resultado.Erros).Campo);
    }

    [Fact]
    public void Faixa_sem_responsavel_nao_recebe_prestacao()
    {
        var (evento, lote) = LoteDeCem();
        lote.Distribuir(evento, 1, 20, null, Agora);

        Assert.False(lote.RegistrarPrestacao(evento, lote.Distribuicoes[0].Id, 20, 0, 500m, null, Agora).Sucesso);
    }

    [Fact]
    public void Novo_lote_do_mesmo_produto_continua_a_numeracao_com_o_preco_vigente()
    {
        var (evento, primeiro) = LoteDeCem();
        var produto = evento.Produtos[0];
        evento.AtualizarProduto(produto.Id, "Feijoada", null, 30.00m, ativo: true, Agora);

        var segundo = LoteTicket.Gerar(evento, produto.Id, 50, Agora).Valor;

        Assert.Equal("101–150", segundo.Faixa.ToString());
        Assert.Equal(30.00m, segundo.PrecoUnitario);
        Assert.Equal(25.00m, primeiro.PrecoUnitario);
        Assert.Equal(151, produto.ProximoNumeroTicket);
    }

    [Fact]
    public void Evento_encerrado_bloqueia_distribuicao_mas_permite_prestacao()
    {
        var (evento, lote) = LoteDeCem();
        ComExemploObrigatorio(evento, lote);

        Assert.True(evento.Encerrar(Agora).Sucesso);

        Assert.False(lote.Distribuir(evento, 80, 90, "Ana", Agora).Sucesso);
        Assert.True(lote.RegistrarPrestacao(evento, lote.Distribuicoes[0].Id, 20, 0, 500m, null, Agora).Sucesso);
        Assert.False(LoteTicket.Gerar(evento, evento.Produtos[0].Id, 10, Agora).Sucesso);
    }

    [Fact]
    public void Evento_valida_datas_e_produtos()
    {
        var comunidade = Criar.Comunidade(2);

        var datas = Evento.Criar("Festa", null, comunidade, new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 19), null, Agora);
        var evento = NovoEvento();
        var duplicado = evento.AdicionarProduto("feijoada", null, 10m, Agora);
        var precoInvalido = evento.AdicionarProduto("Água", null, 4.555m, Agora);

        Assert.Equal("DataFim", Assert.Single(datas.Erros).Campo);
        Assert.Equal("Nome", Assert.Single(duplicado.Erros).Campo);
        Assert.Equal("Preco", Assert.Single(precoInvalido.Erros).Campo);
        Assert.Equal(TipoErro.Validacao, precoInvalido.Erros[0].Tipo);
    }
}
