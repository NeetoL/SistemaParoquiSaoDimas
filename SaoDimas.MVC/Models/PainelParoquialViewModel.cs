using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.MVC.Models;
public sealed record PainelParoquialViewModel(int Dizimistas, IReadOnlyList<ComunidadeDto> Comunidades, PainelEventosDto PainelEventos);

public sealed record MesFinanceiroPainel(DateOnly Mes, decimal Entradas, decimal Saidas, int Pagamentos);
public sealed record FinanceiroPainel(bool Completo, IReadOnlyList<MesFinanceiroPainel> Meses)
{
    public IReadOnlyList<FormaPagamentoPainel> Formas { get; init; } = [];
}
public sealed record FormaPagamentoPainel(string Forma, decimal Valor);
