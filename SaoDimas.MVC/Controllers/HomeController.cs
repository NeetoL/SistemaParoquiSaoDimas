using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.CrossCutting.Filters;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
[TypeFilter(typeof(EstadoEventosNavegadorFilter))]
public sealed class HomeController(IDizimistaAplicacao dizimistas, IComunidadeAplicacao comunidades, IEventoAplicacao eventos, IGestaoParoquialDados gestao, IGestaoParoquialAplicacao rotina) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int? ano = null)
    {
        var cadastro = await dizimistas.PesquisarAsync(new FiltroDizimistas { TamanhoPagina = 1 }, cancellationToken);
        var lista = await comunidades.ListarTodasAsync(cancellationToken);
        var painel = await eventos.ObterPainelAsync(new FiltroEventos(), cancellationToken);
        var registros = await gestao.RegistrosAsync(cancellationToken);
        ViewData["ContagensRotina"] = registros.Where(r => !r.Cancelado).GroupBy(r => r.Modulo).ToDictionary(g => g.Key, g => g.Count());
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
        var agenda = (await rotina.ListarAsync("agenda", null, hoje, null, null, cancellationToken)).OrderBy(r => r.Campos["inicio"]).ToArray();
        ViewData["HojePainel"] = hoje;
        ViewData["AgendaSemana"] = agenda.Where(r => DateOnly.FromDateTime(DateTime.Parse(r.Campos["inicio"], System.Globalization.CultureInfo.InvariantCulture)) <= hoje.AddDays(6)).ToArray();
        ViewData["ProximosCompromissos"] = agenda.Take(4).ToArray();
        var anoPainel = ano is >= 2000 and <= 2100 ? ano.Value : hoje.Year;
        ViewData["AnoPainel"] = anoPainel;
        var perfil = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var completo = CatalogoParoquial.Permite(perfil, "financeiro");
        if (completo || CatalogoParoquial.Permite(perfil, "contribuicoes"))
        {
            var primeiro = new DateOnly(anoPainel, 1, 1);
            var movimentos = await rotina.ListarAsync(completo ? "financeiro" : "contribuicoes", null, primeiro, anoPainel < hoje.Year ? new DateOnly(anoPainel,12,31) : hoje, null, cancellationToken);
            var meses = Enumerable.Range(0, 12).Select(i =>
            {
                var mes = primeiro.AddMonths(i);
                var itens = movimentos.Where(r => DateOnly.TryParse(r.Campos.GetValueOrDefault("data"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dia) && dia >= mes && dia < mes.AddMonths(1)).ToArray();
                decimal Valor(RegistroParoquial r) => decimal.Parse(r.Campos["valor"], System.Globalization.CultureInfo.InvariantCulture);
                var entradas = itens.Where(r => r.Modulo == "contribuicoes" || r.Campos.GetValueOrDefault("natureza") == "Entrada").ToArray();
                return new MesFinanceiroPainel(mes, entradas.Sum(Valor), itens.Where(r => r.Campos.GetValueOrDefault("natureza") == "Saída").Sum(Valor), entradas.Length);
            }).ToArray();
            var formas = movimentos.Where(r => r.Modulo == "contribuicoes" || r.Campos.GetValueOrDefault("natureza") == "Entrada")
                .GroupBy(r => r.Campos.GetValueOrDefault("forma", "Não informada").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new FormaPagamentoPainel(string.Equals(g.Key, "pix", StringComparison.OrdinalIgnoreCase) ? "PIX" : g.Key, g.Sum(r => decimal.Parse(r.Campos["valor"], System.Globalization.CultureInfo.InvariantCulture))))
                .Where(f => f.Valor > 0).OrderByDescending(f => f.Valor).ToArray();
            ViewData["FinanceiroPainel"] = new FinanceiroPainel(completo, meses) { Formas = formas };
        }
        return View(new PainelParoquialViewModel(cadastro.Total, lista, painel));
    }
}
