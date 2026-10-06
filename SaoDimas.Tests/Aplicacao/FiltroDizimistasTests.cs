using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Tests.Aplicacao;

public sealed class FiltroDizimistasTests
{
    [Fact]
    public void Valores_fora_dos_limites_recebidos_do_navegador_sao_corrigidos()
    {
        var filtro = new FiltroDizimistas
        {
            Busca = "   ",
            ComunidadeId = 0,
            Ordenacao = (OrdenacaoDizimistas)99,
            Pagina = -5,
            TamanhoPagina = 10_000
        }.Normalizado();

        Assert.Null(filtro.Busca);
        Assert.Null(filtro.ComunidadeId);
        Assert.Equal(OrdenacaoDizimistas.Nome, filtro.Ordenacao);
        Assert.Equal(1, filtro.Pagina);
        Assert.Equal(FiltroDizimistas.TamanhoPaginaMaximo, filtro.TamanhoPagina);
    }

    [Theory]
    [InlineData(0, 20, 1, 0, 0)]
    [InlineData(45, 20, 3, 41, 45)]
    [InlineData(45, 20, 2, 21, 40)]
    public void Pagina_informa_intervalo_exibido(int total, int tamanho, int pagina, int primeiro, int ultimo)
    {
        var resultado = new PaginaResultado<int>([], pagina, tamanho, total);

        Assert.Equal(primeiro, resultado.PrimeiroItem);
        Assert.Equal(ultimo, resultado.UltimoItem);
    }
}
