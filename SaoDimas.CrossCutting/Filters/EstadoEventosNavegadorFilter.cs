using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using SaoDimas.Aplicacao.Services.Interface;

namespace SaoDimas.CrossCutting.Filters;

/// <summary>Usa o JSON do servidor; documentos do navegador servem somente à importação inicial.</summary>
public sealed class EstadoEventosNavegadorFilter(IEstadoEventosServidor estado) : IAsyncActionFilter
{
    public const string CampoFormulario = "__estadoEventos";
    public const string CabecalhoResposta = "X-Estado-Eventos";
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var requisicao = context.HttpContext.Request;
        string? legado = null;
        if (HttpMethods.IsPost(requisicao.Method) && requisicao.HasFormContentType)
        {
            var formulario = await requisicao.ReadFormAsync(context.HttpContext.RequestAborted);
            legado = formulario[CampoFormulario].ToString();
        }
        if (!await estado.CarregarAsync(legado, context.HttpContext.RequestAborted))
        {
            context.Result = new ContentResult { StatusCode = 400, Content = "Os dados antigos deste navegador são inválidos. Nenhum arquivo foi sobrescrito.", ContentType = "text/plain; charset=utf-8" };
            return;
        }
        context.HttpContext.Response.Headers["X-Persistencia-Eventos"] = "servidor";
        var resultado = await next();
        if (resultado.Exception is null || resultado.ExceptionHandled)
            await estado.SalvarAsync(context.HttpContext.RequestAborted);
    }
}
