using Microsoft.EntityFrameworkCore;
using SaoDimas.CrossCutting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaoDimas.Aplicacao;
using SaoDimas.Infraestrutura;
using SaoDimas.Infraestrutura.Persistencia;

namespace SaoDimas.Tests.Arquitetura;

/// <summary>
/// Valida os registros de AddAplicacao() + AddInfraestrutura() sem depender do SQL Server.
/// </summary>
public sealed class InjecaoDeDependenciaTests
{
    private static ServiceCollection CriarServicos()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SaoDimas"] = "Server=teste;Database=SaoDimas;Integrated Security=True",
                ["Paroquia:Nome"] = "Paróquia de Teste",
                ["Paroquia:CaminhoLogoImpressao"] = "logo.png"
            })
            .Build();

        var services = new ServiceCollection();

        // Serviço fornecido pelo host web (ASP.NET Core), do qual a Infraestrutura depende.
        services.AddSingleton<IHostEnvironment>(new AmbienteDeTeste());

        services
            .AddAplicacao()
            .AddInfraestrutura(configuration)
            .AddCrossCutting(configuration);

        return services;
    }

    [Fact]
    public void Container_e_construido_sem_registros_ausentes_ou_lifetimes_incompativeis()
    {
        var services = CriarServicos();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(provider);
    }

    [Fact]
    public void Todo_contrato_em_namespace_Interface_possui_registro()
    {
        var registrados = CriarServicos()
            .Select(descritor => descritor.ServiceType)
            .ToHashSet();

        var semRegistro = Camadas.Tipos(Camadas.Dominio, Camadas.Aplicacao, Camadas.Infraestrutura)
            .Where(tipo => tipo.IsInterface && tipo.EstaEm(Camadas.SufixoInterface))
            .Where(contrato => !registrados.Contains(contrato))
            .Select(contrato => contrato.FullName);

        Assert.Empty(semRegistro);
    }

    [Fact]
    public void Configuracao_da_paroquia_e_validada()
    {
        using var provider = CriarServicos().BuildServiceProvider();

        Assert.Equal("Paróquia de Teste", provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SaoDimas.Aplicacao.Configuracoes.ConfiguracaoParoquia>>().Value.Nome);
    }

    [Fact]
    public void DbContext_e_registrado_como_Scoped()
    {
        var descritor = Assert.Single(CriarServicos(), d => d.ServiceType == typeof(SaoDimasDbContext));

        Assert.Equal(ServiceLifetime.Scoped, descritor.Lifetime);
    }
}
