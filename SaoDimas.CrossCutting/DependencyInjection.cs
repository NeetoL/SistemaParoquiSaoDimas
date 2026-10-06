using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.CrossCutting.Services.Implementation;
namespace SaoDimas.CrossCutting;
public static class DependencyInjection
{
    private static readonly string[] PrefixosPublicos = ["/css/","/js/","/img/","/icons/","/fonts/","/lib/"];
    public static IServiceCollection AddCrossCutting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConfiguracaoLogin>()
            .Bind(configuration.GetSection(ConfiguracaoLogin.Secao))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IPasswordHasher<ConfiguracaoLogin>, PasswordHasher<ConfiguracaoLogin>>();
        services.AddSingleton<ICredenciaisService, CredenciaisService>();
        services.AddSingleton<ILoginAutenticador, LoginAutenticador>();
        return services;
    }
    public static IServiceCollection AddAutenticacaoWeb(this IServiceCollection services, bool desenvolvimento)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.LoginPath = "/Conta/Login";
            options.AccessDeniedPath = "/Conta/AcessoNegado";
            options.Events.OnValidatePrincipal = async contexto => {
                if(PrefixosPublicos.Any(prefixo=>contexto.HttpContext.Request.Path.StartsWithSegments(prefixo.TrimEnd('/'))))return;
                var identificador=contexto.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var usuarios=contexto.HttpContext.RequestServices.GetRequiredService<IUsuariosAplicacao>();
                var usuario=(await usuarios.ListarAsync(contexto.HttpContext.RequestAborted)).FirstOrDefault(u=>u.Id.ToString()==identificador);
                if(usuario is null||!usuario.Ativo||usuario.Versao.ToString(System.Globalization.CultureInfo.InvariantCulture)!=contexto.Principal?.FindFirst("versao")?.Value){contexto.RejectPrincipal();await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);}
            };
            options.Cookie.Name = "SaoDimas.Sessao";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = desenvolvimento ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
        });
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsync("Muitas tentativas de acesso. Aguarde um minuto e tente novamente.", cancellationToken);
            };
        });
        return services;
    }
}
