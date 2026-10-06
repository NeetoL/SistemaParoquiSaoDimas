using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Aplicacao.Services.Interface;

/// <summary>
/// Gera o PDF A4 dos tickets (vários por folha, cada um com parte do cliente e canhoto). Implementado na Infraestrutura.
/// </summary>
public interface IImpressaoTicketService
{
    byte[] Gerar(ImpressaoTicketsDto impressao);
}
