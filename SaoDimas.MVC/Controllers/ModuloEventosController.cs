using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Dominio;
using SaoDimas.CrossCutting.Filters;

namespace SaoDimas.MVC.Controllers;

/// <summary>
/// Base dos controllers do módulo de Eventos: persistência JSON no servidor e respostas HTMX.
/// </summary>
[TypeFilter(typeof(EstadoEventosNavegadorFilter))]
public abstract class ModuloEventosController : Controller
{
    /// <summary>Recarrega painel/detalhes/lote na página atual.</summary>
    protected const string EventoAtualizar = "eventos:atualizar";

    protected const string EventoAbrirModal = "eventos:abrir-modal";

    private const string EventoFecharModal = "eventos:fechar-modal";

    /// <summary>
    /// Conteúdo carregado dentro do modal do módulo (que é aberto ao chegar).
    /// </summary>
    protected PartialViewResult Modal(string view, object modelo)
    {
        Disparar(new Dictionary<string, object> { [EventoAbrirModal] = true });
        return PartialView(view, modelo);
    }

    /// <summary>
    /// Operação concluída: fecha o modal, atualiza a página e mostra o toast (sem trocar conteúdo).
    /// </summary>
    protected ContentResult Concluido(string mensagem)
    {
        Response.Headers["HX-Reswap"] = "none";
        Disparar(new Dictionary<string, object>
        {
            [EventoFecharModal] = true,
            [EventoAtualizar] = true,
            ["toast"] = new { mensagem, tipo = "success" }
        });
        return Content(string.Empty);
    }

    /// <summary>Navegação após a operação (o documento atualizado já vai no cabeçalho da mesma resposta).</summary>
    protected ContentResult Redirecionar(string url, string mensagem)
    {
        TempData[Models.MensagemTempData.Sucesso] = mensagem;
        Response.Headers["HX-Redirect"] = url;
        return Content(string.Empty);
    }

    protected IActionResult NaoEncontradoOuErro(Resultado resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        return resultado.Erros.Any(erro => erro.Tipo == TipoErro.NaoEncontrado)
            ? NotFound(resultado.Erros[0].Mensagem)
            : BadRequest(string.Join(" ", resultado.Erros.Select(erro => erro.Mensagem)));
    }

    protected void AdicionarErros(Resultado resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        foreach (var erro in resultado.Erros)
        {
            ModelState.AddModelError(erro.Campo, erro.Mensagem);
        }
    }

    // JSON escapa caracteres não ASCII (ç...): cabeçalhos HTTP aceitam apenas ASCII.
    private void Disparar(Dictionary<string, object> eventos) =>
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(eventos);
}
