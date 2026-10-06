using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;

namespace SaoDimas.MVC.Controllers;

/// <summary>
/// Eventos e seus produtos. As páginas (GET) são cascas; o conteúdo é carregado por HTMX (POST) junto com os
/// dados guardados no servidor.
/// </summary>
[Route("Eventos")]
public sealed class EventosController(IEventoAplicacao eventos, IComunidadeAplicacao comunidades) : ModuloEventosController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await comunidades.ListarTodasAsync(cancellationToken));

    [HttpPost("Painel")]
    public async Task<IActionResult> Painel([FromForm] FiltroEventos filtro, CancellationToken cancellationToken) =>
        PartialView("_Painel", await eventos.ObterPainelAsync(filtro ?? new FiltroEventos(), cancellationToken));

    [HttpGet("Novo")]
    public async Task<IActionResult> Novo(CancellationToken cancellationToken)
    {
        var formulario = new EventoFormularioViewModel { DataInicio = DateOnly.FromDateTime(DateTime.Today) };
        await CarregarComunidadesAsync(formulario, cancellationToken);
        return View(formulario);
    }

    [HttpPost("Modelos")]
    public async Task<IActionResult> Modelos(int? modeloEdicaoId, CancellationToken cancellationToken)
    {
        ViewData["ModeloEdicaoId"] = modeloEdicaoId;
        return PartialView("_Modelos", await eventos.ObterPainelAsync(new FiltroEventos(), cancellationToken));
    }

    [HttpPost("Novo")]
    public async Task<IActionResult> Novo(EventoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (ModelState.IsValid)
        {
            var dados = formulario.ParaDados() with { Operador = User.Identity?.IsAuthenticated == true ? User.Identity.Name : formulario.Operador };
            var resultado = await eventos.CriarAsync(dados, cancellationToken);
            if (resultado.Sucesso)
            {
                return Redirecionar(Url.Action(nameof(Detalhes), new { id = resultado.Valor })!, "Evento criado com sucesso.");
            }

            AdicionarErros(resultado);
        }

        await CarregarComunidadesAsync(formulario, cancellationToken);
        return PartialView("_FormularioEvento", formulario);
    }

    [HttpGet("{id:int}")]
    public IActionResult Detalhes(int id, bool editar = false)
    {
        ViewData["AbrirEdicao"] = editar;
        return View(id);
    }

    [HttpPost("{id:int}/Conteudo")]
    public async Task<IActionResult> Conteudo(int id, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterDetalhesAsync(id, cancellationToken);
        return evento is null ? PartialView("_NaoEncontrado") : PartialView("_Detalhes", evento);
    }

    [HttpPost("{id:int}/Editar")]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        var dados = await eventos.ObterParaEdicaoAsync(id, cancellationToken);
        if (dados is null)
        {
            return NotFound("Evento não encontrado.");
        }

        var formulario = EventoFormularioViewModel.De(id, dados);
        await CarregarComunidadesAsync(formulario, cancellationToken);
        return Modal("_FormularioEvento", formulario);
    }

    [HttpPost("{id:int}/Salvar")]
    public async Task<IActionResult> Salvar(int id, EventoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        formulario.Id = id;

        if (ModelState.IsValid)
        {
            var dados = formulario.ParaDados() with { Operador = User.Identity?.IsAuthenticated == true ? User.Identity.Name : formulario.Operador };
            var resultado = await eventos.AtualizarAsync(id, dados, cancellationToken);
            if (resultado.Sucesso)
            {
                return Concluido("Evento atualizado com sucesso.");
            }

            AdicionarErros(resultado);
        }

        await CarregarComunidadesAsync(formulario, cancellationToken);
        return PartialView("_FormularioEvento", formulario);
    }

    [HttpPost("{id:int}/Status")]
    public async Task<IActionResult> Status(int id, AcaoStatusEvento acao, string operador, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operador)) return BadRequest("Informe o operador das ações de status.");
        var resultado = await eventos.AlterarStatusAsync(id, acao, cancellationToken,
            User.Identity?.IsAuthenticated == true ? User.Identity.Name : operador.Trim());

        return resultado.Sucesso
            ? Concluido(acao switch
            {
                AcaoStatusEvento.Ativar => "Evento ativado.",
                AcaoStatusEvento.Encerrar => "Evento encerrado.",
                _ => "Evento cancelado."
            })
            : NaoEncontradoOuErro(resultado);
    }

    [HttpPost("{id:int}/Produtos/Formulario")]
    public async Task<IActionResult> FormularioProduto(int id, int? produtoId, CancellationToken cancellationToken)
    {
        if (produtoId is not int existente)
        {
            return Modal("_FormularioProduto", new ProdutoFormularioViewModel { EventoId = id });
        }

        var dados = await eventos.ObterProdutoAsync(id, existente, cancellationToken);
        return dados is null
            ? NotFound("Produto não encontrado.")
            : Modal("_FormularioProduto", ProdutoFormularioViewModel.De(id, existente, dados));
    }

    [HttpPost("{id:int}/Produtos/Salvar")]
    public async Task<IActionResult> SalvarProduto(int id, ProdutoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        formulario.EventoId = id;

        if (!ValorMonetario.TentarLer(formulario.Preco, out var preco))
        {
            ModelState.AddModelError(nameof(formulario.Preco), "Informe um preço válido. Ex.: 25,00");
        }

        if (ModelState.IsValid)
        {
            var dados = new DadosProduto(formulario.Nome, formulario.Descricao, preco, formulario.Ativo,
                User.Identity?.IsAuthenticated == true ? User.Identity.Name : formulario.Operador);
            var resultado = formulario.ProdutoId is int produtoId
                ? await eventos.AtualizarProdutoAsync(id, produtoId, dados, cancellationToken)
                : await eventos.AdicionarProdutoAsync(id, dados, cancellationToken);

            if (resultado.Sucesso)
            {
                return Concluido(formulario.ProdutoId is null ? "Produto cadastrado." : "Produto atualizado.");
            }

            AdicionarErros(resultado);
        }

        return PartialView("_FormularioProduto", formulario);
    }

    private async Task CarregarComunidadesAsync(EventoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        var disponiveis = await comunidades.ListarParaVinculoAsync(formulario.ComunidadeId, cancellationToken);
        formulario.Comunidades = disponiveis
            .Select(comunidade => new SelectListItem(comunidade.Nome, comunidade.Id.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }
}
