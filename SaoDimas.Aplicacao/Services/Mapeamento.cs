using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Dominio.Entities;

namespace SaoDimas.Aplicacao.Services;

internal static class Mapeamento
{
    public static PrestacaoDto? Prestacao(PrestacaoContas? prestacao) =>
        prestacao is null
            ? null
            : new PrestacaoDto(
                prestacao.Recebidos, prestacao.Vendidos, prestacao.Devolvidos, prestacao.PrecoUnitario, prestacao.ValorEsperado,
                prestacao.ValorEntregue, prestacao.Diferenca, prestacao.Justificativa, prestacao.RegistradaEm);
}
