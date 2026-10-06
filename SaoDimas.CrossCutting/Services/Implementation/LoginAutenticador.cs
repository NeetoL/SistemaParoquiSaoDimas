using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.CrossCutting.Services.Implementation;
internal sealed class LoginAutenticador(IOptions<ConfiguracaoLogin> opcoes, IPasswordHasher<ConfiguracaoLogin> hasher) : ILoginAutenticador
{
    public string Usuario => opcoes.Value.Usuario;
    public bool Validar(string usuario, string senha)
    {
        var configuracao = opcoes.Value;
        // Verifica o hash também para usuários inexistentes, sem comparar senhas em texto puro.
        var senhaValida = hasher.VerifyHashedPassword(configuracao, configuracao.SenhaHash, senha)
            != PasswordVerificationResult.Failed;
        return string.Equals(usuario.Trim(), configuracao.Usuario, StringComparison.OrdinalIgnoreCase) && senhaValida;
    }
}
