using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.MVC.Models;

public sealed class EventoFormularioViewModel
{
    public int? Id { get; set; }
    public int? ModeloEdicaoId { get; set; }
    [Required(ErrorMessage = "Informe quem está registrando a alteração.")]
    [StringLength(150)]
    public string Operador { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome do evento.")]
    [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Selecione a comunidade.")]
    public int? ComunidadeId { get; set; }

    [Required(ErrorMessage = "Informe a data inicial.")]
    public DateOnly? DataInicio { get; set; }

    public DateOnly? DataFim { get; set; }

    [StringLength(2000, ErrorMessage = "As observações devem ter no máximo 2000 caracteres.")]
    public string? Observacoes { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Comunidades { get; set; } = [];

    public static EventoFormularioViewModel De(int id, DadosEvento dados) => new()
    {
        Id = id,
        Nome = dados.Nome,
        Descricao = dados.Descricao,
        ComunidadeId = dados.ComunidadeId,
        DataInicio = dados.DataInicio,
        DataFim = dados.DataFim,
        Observacoes = dados.Observacoes
    };

    public DadosEvento ParaDados() => new(Nome, Descricao, ComunidadeId!.Value, DataInicio!.Value, DataFim, Observacoes, ModeloEdicaoId, Operador);
}

public sealed class ProdutoFormularioViewModel
{
    [Required(ErrorMessage = "Informe quem está registrando o produto.")]
    [StringLength(150)]
    public string Operador { get; set; } = string.Empty;
    public int EventoId { get; set; }

    public int? ProdutoId { get; set; }

    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    /// <summary>Texto como digitado ("25,00" ou "25.00"), convertido por <see cref="ValorMonetario"/>.</summary>
    [Required(ErrorMessage = "Informe o preço.")]
    public string? Preco { get; set; }

    public bool Ativo { get; set; } = true;

    public static ProdutoFormularioViewModel De(int eventoId, int produtoId, DadosProduto dados) => new()
    {
        EventoId = eventoId,
        ProdutoId = produtoId,
        Nome = dados.Nome,
        Descricao = dados.Descricao,
        Preco = ValorMonetario.Formatar(dados.Preco),
        Ativo = dados.Ativo
    };
}

public sealed class LoteFormularioViewModel
{
    [Range(1, 5000, ErrorMessage = "Informe uma quantidade entre 1 e 5000.")]
    [Required(ErrorMessage = "Informe a quantidade de tickets.")]
    public int? Quantidade { get; set; } = 100;

    [ValidateNever]
    public PreparacaoLoteDto Preparacao { get; set; } = default!;

    [ValidateNever]
    public SimulacaoLoteDto? Simulacao { get; set; }
}

public sealed class DistribuicaoFormularioViewModel
{
    [Required(ErrorMessage = "Informe quem está distribuindo os tickets.")]
    [StringLength(150)]
    public string Operador { get; set; } = string.Empty;
    public int? DistribuicaoId { get; set; }

    [Required(ErrorMessage = "Informe o número inicial.")]
    public int? NumeroInicial { get; set; }

    [Required(ErrorMessage = "Informe o número final.")]
    public int? NumeroFinal { get; set; }

    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string? Responsavel { get; set; }

    [ValidateNever]
    public LoteDetalhesDto Lote { get; set; } = default!;
}

public sealed class PrestacaoFormularioViewModel
{
    [Required(ErrorMessage = "Informe a quantidade vendida.")]
    [Range(0, int.MaxValue, ErrorMessage = "Informe zero ou mais.")]
    public int? Vendidos { get; set; }

    [Required(ErrorMessage = "Informe a quantidade devolvida.")]
    [Range(0, int.MaxValue, ErrorMessage = "Informe zero ou mais.")]
    public int? Devolvidos { get; set; }

    [Required(ErrorMessage = "Informe o valor entregue.")]
    public string? ValorEntregue { get; set; }

    [StringLength(500, ErrorMessage = "A justificativa deve ter no máximo 500 caracteres.")]
    public string? Justificativa { get; set; }

    [ValidateNever]
    public PreparacaoPrestacaoDto Preparacao { get; set; } = default!;

    [ValidateNever]
    public SimulacaoPrestacaoDto? Simulacao { get; set; }
}

/// <summary>
/// Valores digitados em reais. Aceita "25,00", "1.250,00" e também "25.00" (teclados numéricos):
/// evita o erro clássico de "25.00" virar 2.500 ao ser lido em português.
/// </summary>
public static class ValorMonetario
{
    private static readonly CultureInfo PortuguesBrasil = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarLer(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpo = texto.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(" ", string.Empty, StringComparison.Ordinal);
        var cultura = limpo.Contains(',', StringComparison.Ordinal) || !limpo.Contains('.', StringComparison.Ordinal)
            ? PortuguesBrasil
            : limpo.Count(c => c == '.') == 1 && limpo.Length - limpo.IndexOf('.', StringComparison.Ordinal) <= 3
                ? CultureInfo.InvariantCulture
                : PortuguesBrasil;

        return decimal.TryParse(limpo, NumberStyles.Number, cultura, out valor);
    }

    public static string Formatar(decimal valor) => valor.ToString("N2", PortuguesBrasil);
}
