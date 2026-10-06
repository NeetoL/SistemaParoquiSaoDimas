using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.MVC.Models;

public sealed class RifaFormularioViewModel
{
    [Required, StringLength(150)] public string Nome { get; set; } = "";
    [Required, StringLength(2000)] public string Premios { get; set; } = "";
    [DataType(DataType.Date)] public DateOnly DataSorteio { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    [Range(typeof(decimal), "0.01", "100000", ParseLimitsInInvariantCulture = true)] public decimal Valor { get; set; }
    [Range(1, 10000)] public int Quantidade { get; set; } = 100;
    [Range(1, int.MaxValue)] public int ComunidadeId { get; set; }
    public IReadOnlyList<SelectListItem> Comunidades { get; set; } = [];
}
public sealed record RifaDetalhesViewModel(RifaDto Rifa, string? Busca, string? Status, int Pagina, IReadOnlyList<int> Numeros, int Total, IReadOnlyList<ComunidadeDto> Comunidades, string? Erro = null, string ReservaNumeros = "", string Comprador = "", string Telefone = "", string Vendedor = "");
