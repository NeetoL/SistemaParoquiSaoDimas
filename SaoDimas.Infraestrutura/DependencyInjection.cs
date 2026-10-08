using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Infraestrutura.Persistencia;
using SaoDimas.Infraestrutura.Persistencia.Json;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;
using SaoDimas.Infraestrutura.Queries.Implementation;
using SaoDimas.Infraestrutura.Repositories.Implementation;
using SaoDimas.Infraestrutura.Services.Implementation;

namespace SaoDimas.Infraestrutura;

public static class DependencyInjection
{
    private const string NomeConnectionString = "SaoDimas";

    /// <summary>
    /// Registra os serviços da camada de infraestrutura (persistência, integrações externas).
    /// </summary>
    /// <remarks>
    /// Repositórios são Scoped (mesmo lifetime do DbContext); nunca registrar como Singleton
    /// algo que consuma o DbContext.
    /// </remarks>
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(NomeConnectionString);

        // EF Core permanece disponível para exportação do legado e futura migração; o uso diário é JSON.
        if (!string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<SaoDimasDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ArquivoSistemaJson>();
        services.AddScoped<SessaoSistemaJson>();
        services.AddSingleton<IDocumentoParoquialService, DocumentoParoquialService>();
        services.AddScoped<IRifasDados, RifasJsonDados>();
        services.AddScoped<IGestaoParoquialDados, GestaoParoquialDados>();
        services.AddScoped<IComunidadeRepositorio, ComunidadeJsonRepositorio>();
        services.AddScoped<IDizimistaRepositorio, DizimistaJsonRepositorio>();
        services.AddScoped<IDizimistaConsultas, DizimistaJsonConsultas>();
        services.AddScoped<IEstadoEventosServidor, EstadoEventosJson>();

        // Dados institucionais da paróquia (seção "Paroquia"), validados na inicialização.
        services.AddOptions<ConfiguracaoParoquia>()
            .Bind(configuration.GetSection(ConfiguracaoParoquia.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // QuestPDF: Community License — gratuita para organizações sem fins lucrativos
        // (https://www.questpdf.com/license). Singleton: sem estado por request; mantém a logo em memória.
        QuestPDF.Settings.License = LicenseType.Community;
        services.AddSingleton<IEnvelopeDizimoPdfService, EnvelopeDizimoPdfService>();
        services.AddSingleton<IRifaPdfService, RifaPdfService>();
        services.AddSingleton<IImpressaoTicketService, ImpressaoTicketService>();
        services.AddSingleton<IRelatorioFechamentoService, RelatorioFechamentoService>();

        // Eventos e Tickets: persistência TEMPORÁRIA no navegador (um estado por requisição, compartilhado pelos
        // repositórios). Ao migrar para SQL Server, os repositórios passam a usar o DbContext e estas linhas saem.
        services.AddScoped<EstadoEventosNavegador>();
        services.AddScoped<IEstadoEventosNavegador>(provider => provider.GetRequiredService<EstadoEventosNavegador>());
        services.AddScoped<IEventoRepositorio, EventoRepositorio>();
        services.AddScoped<ILoteTicketRepositorio, LoteTicketRepositorio>();

        return services;
    }
}
