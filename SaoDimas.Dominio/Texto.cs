using System.Text.RegularExpressions;

namespace SaoDimas.Dominio;

internal static partial class Texto
{
    public static string SomenteDigitos(string? valor) =>
        string.IsNullOrEmpty(valor) ? string.Empty : NaoDigitos().Replace(valor, string.Empty);

    /// <summary>
    /// Remove espaços nas extremidades e espaços repetidos internos.
    /// </summary>
    public static string NormalizarEspacos(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : EspacosRepetidos().Replace(valor.Trim(), " ");

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitos();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex EspacosRepetidos();
}
