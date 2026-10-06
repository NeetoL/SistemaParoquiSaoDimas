using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Aplicacao.Services.Interface;

/// <summary>
/// Gera o molde do Envelope de Dízimo em PDF (folha A4 em tamanho real). Implementado na Infraestrutura.
/// </summary>
public interface IEnvelopeDizimoPdfService
{
    /// <param name="envelopes">Um envelope por folha A4, na ordem informada.</param>
    byte[] Gerar(IReadOnlyList<DizimistaIdentificacaoDto> envelopes);
}
