using System.Globalization;

namespace SaoDimas.Dominio.ValueObjects;

/// <summary>
/// Intervalo fechado de números de ticket (ex.: 001–020). Tickets são derivados de faixas, não armazenados um a um.
/// </summary>
public sealed record FaixaNumeracao
{
    public const int NumeroMaximo = 999_999;

    private FaixaNumeracao(int inicial, int final)
    {
        Inicial = inicial;
        Final = final;
    }

    public int Inicial { get; }

    public int Final { get; }

    public int Quantidade => Final - Inicial + 1;

    public static Resultado<FaixaNumeracao> Criar(int inicial, int final, string campoInicial = "NumeroInicial", string campoFinal = "NumeroFinal")
    {
        if (inicial < 1)
        {
            return Resultado<FaixaNumeracao>.Falha(new Erro(campoInicial, "O número inicial deve ser maior que zero."));
        }

        if (final < inicial)
        {
            return Resultado<FaixaNumeracao>.Falha(new Erro(campoFinal, "O número final deve ser maior ou igual ao número inicial."));
        }

        if (final > NumeroMaximo)
        {
            return Resultado<FaixaNumeracao>.Falha(new Erro(campoFinal, $"O número final não pode passar de {NumeroMaximo}."));
        }

        return Resultado<FaixaNumeracao>.Ok(new FaixaNumeracao(inicial, final));
    }

    /// <summary>
    /// Recria uma faixa já validada (ex.: lida da persistência).
    /// </summary>
    public static FaixaNumeracao Reconstituir(int inicial, int final) => new(inicial, final);

    public bool Contem(int numero) => numero >= Inicial && numero <= Final;

    public bool Contem(FaixaNumeracao outra)
    {
        ArgumentNullException.ThrowIfNull(outra);
        return outra.Inicial >= Inicial && outra.Final <= Final;
    }

    public bool SobrepoeA(FaixaNumeracao outra)
    {
        ArgumentNullException.ThrowIfNull(outra);
        return Inicial <= outra.Final && outra.Inicial <= Final;
    }

    /// <summary>
    /// Número com zeros à esquerda (mínimo de 3 dígitos), como nos tickets impressos: 1 → "001".
    /// </summary>
    public static string FormatarNumero(int numero) => numero.ToString("D3", CultureInfo.InvariantCulture);

    public override string ToString() => $"{FormatarNumero(Inicial)}–{FormatarNumero(Final)}";
}
