using System.ComponentModel.DataAnnotations;
namespace SaoDimas.MVC.Models;
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(100)]
    [Display(Name = "Usuário")]
    public string Usuario { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(256)]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
