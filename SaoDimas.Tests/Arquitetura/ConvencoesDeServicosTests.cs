using System.Reflection;

namespace SaoDimas.Tests.Arquitetura;

/// <summary>
/// Garante a convenção Interface/ + Implementation/ e a Dependency Inversion entre camadas.
/// Ver docs/arquitetura.md.
/// </summary>
public sealed class ConvencoesDeServicosTests
{
    private const BindingFlags Construtores = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void Namespace_Interface_contem_somente_interfaces_com_prefixo_I()
    {
        var violacoes = Camadas.Tipos(Camadas.Todas)
            .Where(tipo => tipo.EstaEm(Camadas.SufixoInterface))
            .Where(tipo => !tipo.IsInterface || tipo.Name.Length < 2 || tipo.Name[0] != 'I' || !char.IsUpper(tipo.Name[1]))
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Namespace_Implementation_contem_somente_classes_sealed_que_implementam_um_contrato()
    {
        var violacoes = Camadas.Tipos(Camadas.Todas)
            .Where(tipo => tipo.EstaEm(Camadas.SufixoImplementation) && !tipo.IsNested)
            .Where(tipo => !tipo.IsClass
                           || !tipo.IsSealed
                           || !tipo.GetInterfaces().Any(contrato => contrato.EstaEm(Camadas.SufixoInterface)))
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Implementacoes_nao_usam_sufixos_como_Impl_ou_Concrete()
    {
        string[] proibidos = ["Impl", "Concrete"];

        var violacoes = Camadas.Tipos(Camadas.Todas)
            .Where(tipo => tipo.EstaEm(Camadas.SufixoImplementation))
            .Where(tipo => proibidos.Any(termo => tipo.Name.Contains(termo, StringComparison.Ordinal)))
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Implementacoes_da_Aplicacao_e_da_Infraestrutura_nao_sao_publicas()
    {
        // Consumidores só enxergam o contrato; o compilador impede "new DizimistaAplicacao()" fora da camada.
        var violacoes = Camadas.Tipos(Camadas.Aplicacao, Camadas.Infraestrutura)
            .Where(tipo => tipo.EstaEm(Camadas.SufixoImplementation) && tipo.IsPublic)
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Contratos_de_repositorio_ficam_no_Dominio_e_implementacoes_na_Infraestrutura()
    {
        var repositorios = Camadas.Tipos(Camadas.Todas)
            .Where(tipo => tipo.Name.EndsWith("Repositorio", StringComparison.Ordinal));

        var violacoes = repositorios
            .Where(tipo => tipo.IsInterface
                ? tipo.Assembly != Camadas.Dominio || !tipo.EstaEm(".Repositories" + Camadas.SufixoInterface)
                : tipo.Assembly != Camadas.Infraestrutura || !tipo.EstaEm(".Repositories" + Camadas.SufixoImplementation))
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Nenhum_construtor_depende_de_implementacao_concreta()
    {
        var violacoes = Camadas.Tipos(Camadas.Todas)
            .SelectMany(tipo => tipo.GetConstructors(Construtores), (tipo, construtor) => (tipo, construtor))
            .SelectMany(item => item.construtor.GetParameters(), (item, parametro) => (item.tipo, parametro))
            .Where(item => item.parametro.ParameterType.EstaEm(Camadas.SufixoImplementation))
            .Select(item => $"{item.tipo.FullName} -> {item.parametro.ParameterType.FullName}");

        Assert.Empty(violacoes);
    }

    [Fact]
    public void Nenhum_construtor_recebe_IServiceProvider()
    {
        // Service Locator esconde dependências; elas devem aparecer explicitamente no construtor.
        var violacoes = Camadas.Tipos(Camadas.Todas)
            .Where(tipo => tipo.GetConstructors(Construtores)
                .SelectMany(construtor => construtor.GetParameters())
                .Any(parametro => parametro.ParameterType == typeof(IServiceProvider)))
            .Select(tipo => tipo.FullName);

        Assert.Empty(violacoes);
    }
}
