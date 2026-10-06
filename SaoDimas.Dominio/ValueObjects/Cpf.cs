namespace SaoDimas.Dominio.ValueObjects;

/// <summary>
/// CPF válido (dígitos verificadores conferidos), armazenado somente com os 11 dígitos.
/// </summary>
public sealed record Cpf
{
    public const int Tamanho = 11;

    private Cpf(string valor) => Valor = valor;

    public string Valor { get; }

    public string Formatado => $"{Valor[..3]}.{Valor[3..6]}.{Valor[6..9]}-{Valor[9..]}";

    public static Resultado<Cpf> Criar(string? cpf, string campo = "Cpf")
    {
        var digitos = Texto.SomenteDigitos(cpf);

        return EhValido(digitos)
            ? Resultado<Cpf>.Ok(new Cpf(digitos))
            : Resultado<Cpf>.Falha(new Erro(campo, "CPF inválido."));
    }

    /// <summary>
    /// Recria um CPF já validado (ex.: lido do banco).
    /// </summary>
    public static Cpf Reconstituir(string digitos) => new(digitos);

    public override string ToString() => Formatado;

    private static bool EhValido(string digitos)
    {
        if (digitos.Length != Tamanho || digitos.All(d => d == digitos[0]))
        {
            return false;
        }

        return DigitoVerificador(digitos, 9) == digitos[9] - '0'
            && DigitoVerificador(digitos, 10) == digitos[10] - '0';
    }

    private static int DigitoVerificador(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
        {
            soma += (digitos[i] - '0') * (quantidade + 1 - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
