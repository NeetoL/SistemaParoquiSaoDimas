using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.MVC.Models;

/// <summary>
/// Formulário de cadastro/edição. As regras de negócio são validadas no backend (domínio e aplicação);
/// as anotações aqui cobrem apenas preenchimento obrigatório e formato.
/// </summary>
public sealed class DizimistaFormularioViewModel
{
    [Required(ErrorMessage = "Informe o código do outro sistema.")]
    [StringLength(50, ErrorMessage = "O código deve ter no máximo 50 caracteres.")]
    [Display(Name = "Código do dizimista")]
    public string CodigoOriginal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(14, ErrorMessage = "CPF inválido.")]
    [Display(Name = "CPF")]
    public string? Cpf { get; set; }

    [StringLength(15, ErrorMessage = "Telefone inválido.")]
    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    [StringLength(250)]
    [Display(Name = "Endereço")]
    public string? Endereco { get; set; }
    [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "Informe um CEP com 8 dígitos.")]
    [Display(Name = "CEP")]
    public string? Cep { get; set; }
    [StringLength(100)]
    public string? Bairro { get; set; }
    [Display(Name = "Data de nascimento")]
    public DateOnly? DataNascimento { get; set; }

    [Required(ErrorMessage = "Selecione uma comunidade.")]
    [Display(Name = "Comunidade")]
    public int? ComunidadeId { get; set; }

    [Required(ErrorMessage = "Informe a data de entrada.")]
    [Display(Name = "Data de entrada")]
    public DateOnly? DataEntrada { get; set; }

    [Required(ErrorMessage = "Selecione o status.")]
    [Display(Name = "Status")]
    public StatusDizimista? Status { get; set; } = StatusDizimista.Ativo;

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Comunidades { get; set; } = [];

    public static DizimistaFormularioViewModel De(DadosDizimista dados) => new()
    {
        Endereco = dados.Endereco, Cep = dados.Cep, Bairro = dados.Bairro, DataNascimento = dados.DataNascimento,
        Nome = dados.Nome,
        CodigoOriginal = dados.CodigoOriginal ?? string.Empty,
        Cpf = dados.Cpf,
        Telefone = dados.Telefone,
        ComunidadeId = dados.ComunidadeId,
        DataEntrada = dados.DataEntrada,
        Status = dados.Status
    };

    /// <summary>
    /// Chamado somente após ModelState válido (campos obrigatórios preenchidos).
    /// </summary>
    public DadosDizimista ParaDados() =>
        new(Nome, Cpf, Telefone, ComunidadeId!.Value, DataEntrada!.Value, Status!.Value, Endereco, Cep, Bairro, DataNascimento, CodigoOriginal);
}
