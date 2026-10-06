using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.MVC.Models;

namespace SaoDimas.MVC.Controllers;

[Route("Eventos/{id:int}/Gestao")]
public sealed class GestaoEventosController(IGestaoEventoAplicacao gestao, IRelatorioFechamentoService relatorio) : ModuloEventosController
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!ModelState.IsValid) context.Result = BadRequest("Confira os campos informados. Existem valores inválidos no formulário.");
        base.OnActionExecuting(context);
    }
    [HttpPost("")]
    public async Task<IActionResult> Conteudo(int id, CancellationToken ct)
    {
        var dados = await gestao.ObterAsync(id, ct);
        return dados is null ? NotFound("Edição não encontrada.") : PartialView("~/Views/Eventos/_Gestao.cshtml", dados);
    }

    [HttpPost("Edicoes")]
    public async Task<IActionResult> NovaEdicao(int id, int ano, DateOnly inicio, DateOnly? fim, bool produtos, bool precos, string operador, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Operador(operador))) return BadRequest("Informe quem está criando a edição.");
        var resultado = await gestao.NovaEdicaoAsync(id, ano, inicio, fim, produtos, precos, ct, Operador(operador));
        return resultado.Sucesso ? Redirecionar(Url.Action("Detalhes", "Eventos", new { id = resultado.Valor })!, "Edição criada com operação e financeiro zerados.") : NaoEncontradoOuErro(resultado);
    }

    [HttpPost("Abertura")]
    public async Task<IActionResult> Abrir(int id, string troco, string operador, string? observacao, CancellationToken ct) =>
        !ValorMonetario.TentarLer(troco, out var valor) ? BadRequest("Informe um fundo de troco válido.") :
        Responder(await gestao.AbrirAsync(id, valor, Operador(operador), observacao, ct), "Caixa aberto.");
    [HttpPost("Situacao")]
    public async Task<IActionResult> Situacao(int id, int loteId, int faixaId, int vendidos, int devolvidos, string operador, CancellationToken ct) =>
        Responder(await gestao.SituacaoAsync(id, loteId, faixaId, vendidos, devolvidos, Operador(operador), ct), "Vendas e devoluções confirmadas.");
    [HttpPost("Receber")]
    public async Task<IActionResult> Receber(int id, string responsavel, string entregue, FormaRecebimento forma, string operador, string? observacao, Guid requisicaoId, CancellationToken ct) =>
        requisicaoId == Guid.Empty ? BadRequest("Reabra o formulário antes de confirmar a prestação.") :
        !ValorMonetario.TentarLer(entregue, out var valor) ? BadRequest("Informe o valor entregue.") :
        Responder(await gestao.ReceberAsync(id, responsavel, valor, forma, Operador(operador), observacao, ct, requisicaoId), "Prestação recebida. Saldo atualizado.");
    [HttpPost("CancelarTickets")]
    public async Task<IActionResult> CancelarTickets(int id, int loteId, int inicial, int final, string motivo, string operador, CancellationToken ct) =>
        Responder(await gestao.CancelarTicketsAsync(id, loteId, inicial, final, motivo, Operador(operador), ct), "Cancelamento de tickets registrado.");
    [HttpPost("Estorno")]
    public async Task<IActionResult> Estornar(int id, Guid lancamento, string motivo, string operador, CancellationToken ct) =>
        Responder(await gestao.EstornarAsync(id, lancamento, motivo, Operador(operador), ct), "Estorno registrado no histórico.");
    [HttpPost("Conferencia")]
    public async Task<IActionResult> Conferir(int id, string contado, string? justificativa, string operador, CancellationToken ct) =>
        !ValorMonetario.TentarLer(contado, out var valor) ? BadRequest("Informe o dinheiro contado.") :
        Responder(await gestao.ConferirAsync(id, valor, justificativa, Operador(operador), ct), "Conferência registrada.");
    [HttpPost("Fechamento")]
    public async Task<IActionResult> Fechar(int id, bool excepcional, string? motivo, string? justificativa, string operador, CancellationToken ct) =>
        Responder(await gestao.FecharAsync(id, excepcional, motivo, justificativa, Operador(operador), ct), "Caixa fechado e edição protegida.");
    [HttpPost("Relatorio")]
    public async Task<IActionResult> Relatorio(int id, CancellationToken ct)
    {
        var dados = await gestao.ObterAsync(id, ct);
        if (dados?.Caixa.Fechamento is null) return BadRequest("Feche o caixa antes de emitir o relatório.");
        return File(relatorio.Gerar(dados), "application/pdf", $"fechamento-{id}-{dados.Ano}.pdf");
    }
    private string Operador(string? informado) => User.Identity?.IsAuthenticated == true
        ? User.Identity.Name ?? "Usuário autenticado" : informado?.Trim() ?? string.Empty;
    private IActionResult Responder(Resultado resultado, string mensagem) => resultado.Sucesso ? Concluido(mensagem) : NaoEncontradoOuErro(resultado);
}
