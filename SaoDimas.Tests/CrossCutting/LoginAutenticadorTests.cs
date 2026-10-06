using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.CrossCutting.Services.Implementation;

namespace SaoDimas.Tests.CrossCutting;

public sealed class LoginAutenticadorTests
{
    private static ConfiguracaoLogin ConfiguracaoInicial()
    {
        for (var diretorio = new DirectoryInfo(AppContext.BaseDirectory); diretorio is not null; diretorio = diretorio.Parent)
        {
            var caminho = Path.Combine(diretorio.FullName, "SaoDimas.MVC", "appsettings.json");
            if (!File.Exists(caminho)) continue;
            using var json = JsonDocument.Parse(File.ReadAllText(caminho));
            var login = json.RootElement.GetProperty(ConfiguracaoLogin.Secao);
            return new ConfiguracaoLogin
            {
                Usuario = login.GetProperty("Usuario").GetString()!,
                SenhaHash = login.GetProperty("SenhaHash").GetString()!
            };
        }
        throw new InvalidOperationException("Configuração inicial não encontrada.");
    }

    [Theory]
    [InlineData("admin", "admin", true)]
    [InlineData(" ADMIN ", "admin", true)]
    [InlineData("admin", "errada", false)]
    [InlineData("outro", "admin", false)]
    [InlineData("admin", "ADMIN", false)]
    public void Conta_inicial_aceita_somente_credenciais_validas(string usuario, string senha, bool esperado)
    {
        var configuracao = ConfiguracaoInicial();
        var autenticador = new LoginAutenticador(Options.Create(configuracao), new PasswordHasher<ConfiguracaoLogin>());
        Assert.Equal(esperado, autenticador.Validar(usuario, senha));
        Assert.NotEqual("admin", configuracao.SenhaHash);
    }
}
