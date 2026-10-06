using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Tests.Dominio;

public sealed class ValueObjectsTests
{
    // CPF sintético (dígitos verificadores calculados para 123456789), não pertence a ninguém.
    private const string CpfSinteticoValido = "12345678909";

    [Theory]
    [InlineData(CpfSinteticoValido)]
    [InlineData("123.456.789-09")]
    public void Cpf_valido_e_armazenado_somente_com_digitos(string entrada)
    {
        var resultado = Cpf.Criar(entrada);

        Assert.True(resultado.Sucesso);
        Assert.Equal(CpfSinteticoValido, resultado.Valor.Valor);
        Assert.Equal("123.456.789-09", resultado.Valor.Formatado);
    }

    [Theory]
    [InlineData("123.456.789-00")]
    [InlineData("111.111.111-11")]
    [InlineData("1234567890")]
    [InlineData("abc")]
    public void Cpf_invalido_e_rejeitado(string entrada)
    {
        var resultado = Cpf.Criar(entrada);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Cpf", Assert.Single(resultado.Erros).Campo);
    }

    [Theory]
    [InlineData("(11) 98765-4321", "(11) 98765-4321")]
    [InlineData("1133334444", "(11) 3333-4444")]
    public void Telefone_valido_e_formatado(string entrada, string formatado)
    {
        var resultado = Telefone.Criar(entrada);

        Assert.True(resultado.Sucesso);
        Assert.Equal(formatado, resultado.Valor.Formatado);
    }

    [Theory]
    [InlineData("98765-4321")]
    [InlineData("(01) 98765-4321")]
    [InlineData("123456789012")]
    public void Telefone_sem_ddd_ou_invalido_e_rejeitado(string entrada)
    {
        Assert.False(Telefone.Criar(entrada).Sucesso);
    }
}
