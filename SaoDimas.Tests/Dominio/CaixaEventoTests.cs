using SaoDimas.Dominio.Entities;

namespace SaoDimas.Tests.Dominio;

public sealed class CaixaEventoTests
{
    [Fact]
    public void Repetir_confirmacao_nao_duplica_recebimento()
    {
        var caixa = new CaixaEvento(); var agora = RelogioFixo.Padrao; var chave = Guid.NewGuid();
        caixa.Abrir(300m, "José", null, agora);
        Assert.True(caixa.Receber("João", 500m, 1050m, FormaRecebimento.Dinheiro, "José", null, agora, chave).Sucesso);
        var recarregado = CaixaEvento.Reconstituir(caixa.Exportar());
        Assert.True(recarregado.Receber("João", 500m, 1050m, FormaRecebimento.Dinheiro, "José", null, agora, chave).Sucesso);
        Assert.Single(recarregado.Movimentacoes); Assert.Equal(500m, recarregado.Receita);
        Assert.False(recarregado.Receber("João", 100m, 1050m, FormaRecebimento.Pix, "José", null, agora, chave).Sucesso);
    }
    [Fact]
    public void Troco_nao_e_receita_e_diferenca_de_cinquenta_exige_justificativa()
    {
        var caixa = new CaixaEvento(); var agora = RelogioFixo.Padrao;
        Assert.True(caixa.Abrir(300m, "José", null, agora).Sucesso);
        Assert.True(caixa.Receber("João", 5000m, 5000m, FormaRecebimento.Dinheiro, "José", null, agora).Sucesso);
        Assert.Equal(5000m, caixa.Receita); Assert.Equal(5300m, caixa.DinheiroTeorico);
        Assert.False(caixa.Conferir(5250m, null, "José", agora).Sucesso);
        Assert.True(caixa.Conferir(5250m, "Faltaram cinquenta reais", "José", agora).Sucesso);
        Assert.Equal(-50m, caixa.Conferencia!.Contado - caixa.Conferencia.Teorico);
        Assert.True(caixa.Conferir(5300m, null, "José", agora).Sucesso);
        Assert.True(caixa.Fechar([], false, null, null, "José", agora).Sucesso);
        Assert.False(caixa.Receber("Maria", 1m, 1m, FormaRecebimento.Pix, "José", null, agora).Sucesso);
    }

    [Fact]
    public void Valores_invalidos_excesso_e_formas_desconhecidas_sao_rejeitados()
    {
        var caixa = new CaixaEvento(); var agora = RelogioFixo.Padrao;
        Assert.False(caixa.Abrir(300.001m, "José", null, agora).Sucesso);
        Assert.True(caixa.Abrir(300m, "José", null, agora).Sucesso);
        Assert.False(caixa.Receber("João", 100.001m, 1000m, FormaRecebimento.Pix, "José", null, agora).Sucesso);
        Assert.False(caixa.Receber("João", 100m, 1000m, (FormaRecebimento)99, "José", null, agora).Sucesso);
        Assert.False(caixa.Receber("João", 1001m, 1000m, FormaRecebimento.Pix, "José", null, agora).Sucesso);
        Assert.Empty(caixa.Movimentacoes);
    }
}
