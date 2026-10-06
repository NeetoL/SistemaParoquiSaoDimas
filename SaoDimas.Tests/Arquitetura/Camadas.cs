using System.Reflection;
using System.Runtime.CompilerServices;

namespace SaoDimas.Tests.Arquitetura;

internal static class Camadas
{
    public static readonly Assembly Dominio = Assembly.Load("SaoDimas.Dominio");
    public static readonly Assembly Aplicacao = Assembly.Load("SaoDimas.Aplicacao");
    public static readonly Assembly Infraestrutura = Assembly.Load("SaoDimas.Infraestrutura");
    public static readonly Assembly CrossCutting = Assembly.Load("SaoDimas.CrossCutting");
    public static readonly Assembly Mvc = Assembly.Load("SaoDimas.MVC");

    public static readonly Assembly[] Todas = [Dominio, Aplicacao, Infraestrutura, CrossCutting, Mvc];

    public const string SufixoInterface = ".Interface";
    public const string SufixoImplementation = ".Implementation";

    /// <summary>
    /// Tipos escritos no código-fonte (exclui tipos gerados pelo compilador).
    /// </summary>
    public static IEnumerable<Type> Tipos(params Assembly[] assemblies) =>
        assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(tipo => !tipo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                           && !tipo.Name.StartsWith('<'));

    public static bool EstaEm(this Type tipo, string sufixoNamespace) =>
        tipo.Namespace?.EndsWith(sufixoNamespace, StringComparison.Ordinal) == true;
}
