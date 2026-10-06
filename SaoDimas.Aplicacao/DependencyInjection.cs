using Microsoft.Extensions.DependencyInjection;
using SaoDimas.Aplicacao.Services.Implementation;
using SaoDimas.Aplicacao.Services.Interface;

namespace SaoDimas.Aplicacao;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços da camada de aplicação e os Domain Services
    /// (o Domínio não conhece o container de DI).
    /// </summary>
    /// <remarks>
    /// Application Services são Scoped por padrão: participam do request HTTP e consomem repositórios.
    /// </remarks>
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        // Relógio do sistema (Singleton, sem estado): permite controlar "agora" nos testes.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICadastrosPlanilhaAplicacao, CadastrosPlanilhaAplicacao>();
        services.AddScoped<IUsuariosAplicacao, UsuariosAplicacao>();
        services.AddScoped<IGestaoParoquialAplicacao, GestaoParoquialAplicacao>();
        services.AddScoped<IComunidadeAplicacao, ComunidadeAplicacao>();
        services.AddScoped<IDizimistaAplicacao, DizimistaAplicacao>();
        services.AddScoped<IEnvelopeDizimoAplicacao, EnvelopeDizimoAplicacao>();
        services.AddScoped<IEventoAplicacao, EventoAplicacao>();
        services.AddScoped<ITicketAplicacao, TicketAplicacao>();
        services.AddScoped<IGestaoEventoAplicacao, GestaoEventoAplicacao>();

        return services;
    }
}
