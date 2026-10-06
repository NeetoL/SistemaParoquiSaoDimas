using System.ComponentModel.DataAnnotations;
namespace SaoDimas.Aplicacao.Configuracoes;
public sealed class ConfiguracaoLogin
{
    public const string Secao = "Login";
    [Required, StringLength(100)]
    public string Usuario { get; init; } = string.Empty;
    [Required]
    public string SenhaHash { get; init; } = string.Empty;
}
