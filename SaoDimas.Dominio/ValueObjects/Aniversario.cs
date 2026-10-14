using System.Globalization;
namespace SaoDimas.Dominio.ValueObjects;
/// <summary>Dia e mês de aniversário, sem ano de nascimento.</summary>
public readonly record struct Aniversario
{
    public int Dia { get; }
    public int Mes { get; }
    public bool Valido => Mes is >= 1 and <= 12 && Dia >= 1 && Dia <= DiasDoMes(Mes);
    public Aniversario(int dia, int mes)
    {
        Dia = dia; Mes = mes;
        if (!Valido) throw new ArgumentOutOfRangeException(nameof(dia), "Informe um aniversário válido (dia/mês).");
    }
    private static int DiasDoMes(int mes) => mes switch { 2 => 29, 4 or 6 or 9 or 11 => 30, _ => 31 };
    public static bool TryParse(string? texto, out Aniversario valor)
    {
        valor = default;
        texto = texto?.Trim().TrimEnd('/');
        if (DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var antiga))
        { valor = new(antiga.Day, antiga.Month); return true; }
        var partes = texto?.Split('/');
        if (partes is not { Length: 2 } || !int.TryParse(partes[0], out var dia) || !int.TryParse(partes[1], out var mes)) return false;
        if (mes is < 1 or > 12 || dia < 1 || dia > DiasDoMes(mes)) return false;
        valor = new(dia, mes); return true;
    }
    public static Aniversario Parse(string texto) => TryParse(texto, out var valor) ? valor : throw new FormatException("Informe um aniversário válido (dia/mês).");
    public static implicit operator Aniversario(DateOnly data) => new(data.Day, data.Month);
    public override string ToString() => $"{Dia:D2}/{Mes:D2}";
    public string ToString(string formato, IFormatProvider? cultura = null) => ToString();
}
