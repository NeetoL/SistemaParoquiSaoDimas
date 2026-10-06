using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SaoDimas.MVC.UI;

internal static class PaginasEstado
{
    // Personaliza somente respostas vazias solicitadas como página HTML.
    // JSON, documentos, mensagens existentes e fragmentos HTMX mantêm sua resposta.
    internal static async Task RenderizarAsync(HttpContext contexto)
    {
        if (!contexto.Request.Headers.Accept.Any(v => v?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
            || contexto.Request.Headers.ContainsKey("HX-Request")
            || contexto.Response.StatusCode is not (400 or 403 or 404 or 500)) return;

        var acao = new ActionContext(contexto, contexto.GetRouteData(), new ActionDescriptor());
        var engine = contexto.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var pagina = engine.GetView(null, "/Views/Shared/Estado.cshtml", true);
        if (!pagina.Success) return;
        var dados = new ViewDataDictionary<int>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = contexto.Response.StatusCode,
            ["PaginaAvulsa"] = true
        };
        var temporarios = new TempDataDictionary(contexto, contexto.RequestServices.GetRequiredService<ITempDataProvider>());
        using var texto = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        await pagina.View.RenderAsync(new ViewContext(acao, pagina.View, dados, temporarios, texto, new HtmlHelperOptions()));
        contexto.Response.ContentType = "text/html; charset=utf-8";
        await contexto.Response.WriteAsync(texto.ToString(), contexto.RequestAborted);
    }
}

