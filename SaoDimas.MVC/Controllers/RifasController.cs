using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;

public sealed class RifasController(IRifasAplicacao rifas, IComunidadeAplicacao comunidades, IRifaPdfService pdf) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await rifas.ListarAsync(ct));
    private async Task Opcoes(RifaFormularioViewModel model, CancellationToken ct) => model.Comunidades = (await comunidades.ListarTodasAsync(ct)).Where(c => c.Ativa).Select(c => new SelectListItem(c.Nome, c.Id.ToString(CultureInfo.InvariantCulture))).ToArray();
    public async Task<IActionResult> Novo(CancellationToken ct) { var m = new RifaFormularioViewModel(); await Opcoes(m, ct); return View(m); }
    [HttpPost]
    public async Task<IActionResult> Novo(RifaFormularioViewModel m, CancellationToken ct)
    {
        if (ModelState.IsValid) { var res = await rifas.CriarAsync(new(m.Nome, m.Premios, m.DataSorteio, m.Valor, m.Quantidade, m.ComunidadeId), User.Identity!.Name!, ct); if (res.Sucesso) { TempData[MensagemTempData.Sucesso] = res.Mensagem; return RedirectToAction(nameof(Detalhes), new { id = res.Id }); } ModelState.AddModelError("", res.Mensagem); }
        await Opcoes(m, ct); return View(m);
    }
    public async Task<IActionResult> Detalhes(Guid id, string? busca, string? status, int pagina = 1, CancellationToken ct = default)
    {
        var r = await rifas.ObterAsync(id, ct); return r is null ? NotFound() : View(await Modelo(r, busca, status, pagina, ct));
    }
    private async Task<RifaDetalhesViewModel> Modelo(RifaDto r, string? busca, string? status, int pagina, CancellationToken ct)
    {
        var ocupados = r.Numeros.ToDictionary(n => n.Numero); var numeros = Enumerable.Range(1, r.Quantidade).Where(n =>
        {
            ocupados.TryGetValue(n, out var ticket); return (status is null || status == "" || status == "Livre" && ticket is null || status == "Reservado" && ticket is { Pago: false } || status == "Pago" && ticket is { Pago: true }) && (string.IsNullOrWhiteSpace(busca) || n.ToString("D4", CultureInfo.InvariantCulture).Contains(busca, StringComparison.OrdinalIgnoreCase) || ticket?.Comprador.Contains(busca, StringComparison.OrdinalIgnoreCase) == true || ticket?.Vendedor.Contains(busca, StringComparison.OrdinalIgnoreCase) == true);
        }).ToArray(); pagina = Math.Clamp(pagina, 1, Math.Max(1, (numeros.Length + 99) / 100)); return new(r, busca, status, pagina, numeros.Skip((pagina - 1) * 100).Take(100).ToArray(), numeros.Length, await comunidades.ListarTodasAsync(ct));
    }
    private async Task<IActionResult> Resposta(Guid id, ResultadoGestao res, CancellationToken ct, string numeros = "", string comprador = "", string telefone = "", string vendedor = "")
    {
        if (res.Sucesso) { TempData[MensagemTempData.Sucesso] = res.Mensagem; return RedirectToAction(nameof(Detalhes), new { id }); }
        var r = await rifas.ObterAsync(id, ct); if (r is null) return NotFound(); return View("Detalhes", (await Modelo(r, null, null, 1, ct)) with { Erro = res.Mensagem, ReservaNumeros = numeros, Comprador = comprador, Telefone = telefone, Vendedor = vendedor });
    }
    [HttpPost] public async Task<IActionResult> Reservar(Guid id, int revisao, string? numeros, string? comprador, string? telefone, string? vendedor, CancellationToken ct) => await Resposta(id, await rifas.ReservarAsync(id, revisao, numeros ?? "", comprador ?? "", telefone ?? "", vendedor ?? "", User.Identity!.Name!, ct), ct, numeros ?? "", comprador ?? "", telefone ?? "", vendedor ?? "");
    [HttpPost] public async Task<IActionResult> Pagamento(Guid id, int revisao, int numero, bool pago, string? forma, CancellationToken ct) => await Resposta(id, await rifas.PagamentoAsync(id, revisao, numero, pago, forma ?? "", User.Identity!.Name!, ct), ct);
    [HttpPost] public async Task<IActionResult> Liberar(Guid id, int revisao, int numero, CancellationToken ct) => await Resposta(id, await rifas.LiberarAsync(id, revisao, numero, User.Identity!.Name!, ct), ct);
    [HttpPost] public async Task<IActionResult> Situacao(Guid id, int revisao, string situacao, bool confirmar, CancellationToken ct) => await Resposta(id, situacao == "Cancelada" && !confirmar ? new(false, "Confirme o cancelamento.") : await rifas.SituacaoAsync(id, revisao, situacao, User.Identity!.Name!, ct), ct);
    [HttpPost] public async Task<IActionResult> Resultado(Guid id, int revisao, int premio, int numero, string? referencia, bool confirmar, CancellationToken ct) => await Resposta(id, !confirmar ? new(false, "Confirme o registro definitivo do resultado.") : await rifas.ResultadoAsync(id, revisao, premio, numero, referencia ?? "", User.Identity!.Name!, ct), ct);
    public async Task<IActionResult> Exportar(Guid id, CancellationToken ct)
    {
        var r = await rifas.ObterAsync(id, ct); if (r is null) return NotFound(); var csv = new StringBuilder("Número;Comprador;Telefone;Vendedor;Situação;Valor;Forma\n");
        string Q(string s) { if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s; return "\"" + s.Replace("\"", "\"\"") + "\""; }
        foreach (var n in r.Numeros.OrderBy(n => n.Numero)) csv.AppendLine(string.Join(';', new[] { n.Numero.ToString("D4", CultureInfo.InvariantCulture), n.Comprador, n.Telefone, n.Vendedor, n.Pago ? "Pago" : "Reservado", r.Valor.ToString(CultureInfo.InvariantCulture), n.Forma }.Select(Q)));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", "rifa-" + id + ".csv");
    }
    [HttpGet]
    public async Task<IActionResult> Pdf(Guid id, int? inicio, int? fim, CancellationToken ct)
    {
        var r = await rifas.ObterAsync(id, ct); if (r is null) return NotFound();
        var primeiro = inicio ?? 1; var ultimo = fim ?? r.Quantidade;
        if (!ModelState.IsValid || primeiro < 1 || ultimo < primeiro || ultimo > r.Quantidade) { TempData[MensagemTempData.Erro] = "Confira o intervalo de números da rifa."; return RedirectToAction(nameof(Detalhes), new { id }); }
        var comunidade = (await comunidades.ListarTodasAsync(ct)).FirstOrDefault(c => c.Id == r.ComunidadeId)?.Nome ?? "Paróquia São Dimas";
        ct.ThrowIfCancellationRequested();
        return File(pdf.Gerar(r, comunidade, primeiro, ultimo), "application/pdf", $"rifa-{primeiro:D4}-{ultimo:D4}.pdf");
    }
    public async Task<IActionResult> Imprimir(Guid id, int? numero, CancellationToken ct) { var r = await rifas.ObterAsync(id, ct); if (r is null) return NotFound(); if (numero.HasValue && !r.Numeros.Any(n => n.Numero == numero)) return NotFound(); return View(r with { Numeros = r.Numeros.Where(n => numero is null || n.Numero == numero).ToList() }); }
}
