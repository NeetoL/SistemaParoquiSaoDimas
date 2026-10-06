using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Tests.Dominio;

public sealed class ComunidadeTests
{
    [Theory]
    [InlineData(TipoComunidade.Matriz, "Paróquia São Dimas", "Paróquia São Dimas")]
    [InlineData(TipoComunidade.Capela, "Santa Teresinha", "Capela Santa Teresinha")]
    public void Nome_completo_depende_do_tipo(TipoComunidade tipo, string nome, string nomeCompleto)
    {
        Assert.Equal(nomeCompleto, Comunidade.FormatarNome(tipo, nome));
    }

    [Fact]
    public void Nova_comunidade_e_criada_ativa_e_pode_receber_vinculos()
    {
        var comunidade = Comunidade.Criar("  Santo   Inácio ", TipoComunidade.Capela, 3, RelogioFixo.Padrao).Valor;

        Assert.Equal("Santo Inácio", comunidade.Nome);
        Assert.True(comunidade.Ativa);
        Assert.True(comunidade.PodeReceberVinculos);
    }

    [Fact]
    public void Comunidade_inativa_nao_pode_receber_vinculos()
    {
        var comunidade = Criar.Comunidade(2);

        comunidade.Inativar(RelogioFixo.Padrao);

        Assert.False(comunidade.PodeReceberVinculos);
        Assert.NotNull(comunidade.AtualizadoEm);
    }

    [Fact]
    public void Nome_e_tipo_sao_validados()
    {
        var resultado = Comunidade.Criar(" ", (TipoComunidade)99, -1, RelogioFixo.Padrao);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            [nameof(Comunidade.Nome), nameof(Comunidade.Tipo), nameof(Comunidade.OrdemExibicao)],
            resultado.Erros.Select(erro => erro.Campo));
    }
}
