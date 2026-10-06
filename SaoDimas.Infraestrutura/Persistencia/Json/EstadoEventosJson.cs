using System.Text.Json;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal sealed class EstadoEventosJson(SessaoSistemaJson sessao, EstadoEventosNavegador estado) : IEstadoEventosServidor
{
    public async Task<bool> CarregarAsync(string? documentoLegado, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        var documento = sessao.Documento;
        if (!documento.EventosLegadosImportados && documento.Eventos.Eventos.Count == 0 && documento.Eventos.Lotes.Count == 0
            && !string.IsNullOrWhiteSpace(documentoLegado))
        {
            if (!estado.Carregar(documentoLegado)) return false;
            var legado = JsonSerializer.Deserialize<DocumentoEventos>(documentoLegado, ArquivoSistemaJson.Serializacao)!;
            if (legado.Eventos.Count > 0 || legado.Lotes.Count > 0)
            {
                documento.Eventos = legado;
                documento.EventosLegadosImportados = true;
                await sessao.SalvarAsync(ct);
            }
        }
        return estado.Carregar(JsonSerializer.Serialize(documento.Eventos, ArquivoSistemaJson.Serializacao));
    }
    public async Task SalvarAsync(CancellationToken ct)
    {
        if (estado.ObterDocumentoAlterado() is not { } alterado) return;
        sessao.Documento.Eventos = JsonSerializer.Deserialize<DocumentoEventos>(alterado, ArquivoSistemaJson.Serializacao)!;
        sessao.Documento.EventosLegadosImportados = true;
        await sessao.SalvarAsync(ct);
    }
}
