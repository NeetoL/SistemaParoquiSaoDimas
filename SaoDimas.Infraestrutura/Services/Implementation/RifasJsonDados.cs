using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Infraestrutura.Persistencia.Json;
namespace SaoDimas.Infraestrutura.Services.Implementation;

internal sealed class RifasJsonDados(SessaoSistemaJson sessao) : IRifasDados
{
    public async Task<IReadOnlyList<RifaDto>> ListarAsync(CancellationToken ct) { await sessao.CarregarAsync(ct); return sessao.Documento.Rifas.ToArray(); }
    public async Task SalvarAsync(RifaDto rifa, string operador, string acao, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct); var antes = sessao.Documento.Rifas.FirstOrDefault(r => r.Id == rifa.Id); sessao.Documento.Rifas.RemoveAll(r => r.Id == rifa.Id); sessao.Documento.Rifas.Add(rifa);
        sessao.Documento.Auditoria.Add(new(Guid.NewGuid(), DateTime.UtcNow, operador, acao, "rifas", rifa.Id.ToString(), Antes: antes is null ? null : new() { ["rifa"] = System.Text.Json.JsonSerializer.Serialize(antes) }, Depois: new() { ["rifa"] = System.Text.Json.JsonSerializer.Serialize(rifa) }));
        await sessao.SalvarAsync(ct);
    }
}
