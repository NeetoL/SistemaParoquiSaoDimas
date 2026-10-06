namespace SaoDimas.Dominio;

public enum TipoErro
{
    Validacao = 1,
    NaoEncontrado = 2
}

/// <summary>
/// Erro de negócio esperado. <see cref="Campo"/> identifica o dado de origem (vazio quando o erro é geral).
/// </summary>
public sealed record Erro(string Campo, string Mensagem, TipoErro Tipo = TipoErro.Validacao)
{
    public static Erro NaoEncontrado(string mensagem) => new(string.Empty, mensagem, TipoErro.NaoEncontrado);
}

/// <summary>
/// Resultado de uma operação que pode falhar por regra de negócio (sem usar exceções como fluxo).
/// </summary>
public class Resultado
{
    protected Resultado(IReadOnlyList<Erro> erros) => Erros = erros;

    public IReadOnlyList<Erro> Erros { get; }

    public bool Sucesso => Erros.Count == 0;

    public static Resultado Ok() => new([]);

    public static Resultado Falha(params IReadOnlyList<Erro> erros) => new(erros);
}

public sealed class Resultado<T> : Resultado
{
    private readonly T? _valor;

    private Resultado(T valor) : base([]) => _valor = valor;

    private Resultado(IReadOnlyList<Erro> erros) : base(erros) { }

    public T Valor => Sucesso
        ? _valor!
        : throw new InvalidOperationException("Não há valor em um resultado com falha.");

    public static Resultado<T> Ok(T valor) => new(valor);

    public static new Resultado<T> Falha(params IReadOnlyList<Erro> erros) => new(erros);
}
