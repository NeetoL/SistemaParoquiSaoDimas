namespace SaoDimas.Dominio.ValueObjects;

/// <summary>
/// Pessoa que recebe uma faixa de tickets. Não precisa ser dizimista nem usuário do sistema.
/// Hoje é identificada pelo nome; pode evoluir para uma entidade própria (com Id) sem mudar as faixas.
/// </summary>
public sealed record Responsavel
{
    public const int NomeTamanhoMaximo = 100;

    private Responsavel(string nome) => Nome = nome;

    public string Nome { get; }

    /// <summary>
    /// Responsável opcional: nome vazio significa faixa sem responsável (nulo).
    /// </summary>
    public static Resultado<Responsavel?> CriarOpcional(string? nome, string campo = "Responsavel")
    {
        var normalizado = Texto.NormalizarEspacos(nome);
        if (normalizado.Length == 0)
        {
            return Resultado<Responsavel?>.Ok(null);
        }

        return normalizado.Length > NomeTamanhoMaximo
            ? Resultado<Responsavel?>.Falha(new Erro(campo, $"O nome do responsável deve ter no máximo {NomeTamanhoMaximo} caracteres."))
            : Resultado<Responsavel?>.Ok(new Responsavel(normalizado));
    }

    public static Responsavel Reconstituir(string nome) => new(nome);

    /// <summary>
    /// Mesma pessoa, independentemente de maiúsculas/minúsculas ("joão da silva" = "João da Silva").
    /// </summary>
    public bool MesmaPessoa(Responsavel? outro) =>
        outro is not null && string.Equals(Nome, outro.Nome, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Nome;
}
