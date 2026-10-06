namespace SaoDimas.Dominio;

internal static class Monetario
{
    /// <summary>
    /// Valor em reais com no máximo duas casas decimais.
    /// </summary>
    public static bool EhValido(decimal valor) => decimal.Round(valor, 2) == valor;
}
