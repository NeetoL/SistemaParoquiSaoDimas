namespace SaoDimas.Dominio.ValueObjects;

/// <summary>
/// Telefone brasileiro com DDD (fixo: 10 dígitos; celular: 11), armazenado somente com dígitos.
/// </summary>
public sealed record Telefone
{
    public const int TamanhoMaximo = 11;

    private Telefone(string valor) => Valor = valor;

    public string Valor { get; }

    public string Formatado => Formatar(Valor);

    public static Resultado<Telefone> Criar(string? telefone, string campo = "Telefone")
    {
        var digitos = Texto.SomenteDigitos(telefone);

        return digitos.Length is 10 or TamanhoMaximo && digitos[0] != '0'
            ? Resultado<Telefone>.Ok(new Telefone(digitos))
            : Resultado<Telefone>.Falha(new Erro(campo, "Telefone inválido. Informe o DDD e o número."));
    }

    public static Telefone Reconstituir(string digitos) => new(digitos);

    public static string Formatar(string digitos) => digitos.Length switch
    {
        11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
        10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
        _ => digitos
    };

    public override string ToString() => Formatado;
}
