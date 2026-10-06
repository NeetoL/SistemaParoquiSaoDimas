using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.MVC.Models;

namespace SaoDimas.MVC.Controllers;

public sealed class DizimistasController(
    IDizimistaAplicacao dizimistas,
    IComunidadeAplicacao comunidades) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltroDizimistas filtro, CancellationToken cancellationToken)
    {
        var filtroNormalizado = (filtro ?? new FiltroDizimistas()).Normalizado();

        var resultado = await dizimistas.PesquisarAsync(filtroNormalizado, cancellationToken);
        var todasComunidades = await comunidades.ListarTodasAsync(cancellationToken);

        return View(new DizimistasIndexViewModel(filtroNormalizado, resultado, todasComunidades));
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes(int id, CancellationToken cancellationToken)
    {
        var dizimista = await dizimistas.ObterDetalhesAsync(id, cancellationToken);

        return dizimista is null ? NotFound() : View(dizimista);
    }

    [HttpGet]
    public async Task<IActionResult> Novo(CancellationToken cancellationToken)
    {
        var formulario = new DizimistaFormularioViewModel { DataEntrada = DateOnly.FromDateTime(DateTime.Today) };
        await CarregarComunidadesAsync(formulario, comunidadeAtualId: null, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    public async Task<IActionResult> Novo(DizimistaFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (ModelState.IsValid)
        {
            var resultado = await dizimistas.CadastrarAsync(formulario.ParaDados(), cancellationToken);

            if (resultado.Sucesso)
            {
                TempData[MensagemTempData.Sucesso] = "Dizimista cadastrado com sucesso.";
                return RedirectToAction(nameof(Detalhes), new { id = resultado.Valor });
            }

            AdicionarErros(resultado);
        }

        await CarregarComunidadesAsync(formulario, comunidadeAtualId: null, cancellationToken);
        return View(formulario);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        var dados = await dizimistas.ObterParaEdicaoAsync(id, cancellationToken);
        if (dados is null)
        {
            return NotFound();
        }

        var formulario = DizimistaFormularioViewModel.De(dados);
        await CarregarComunidadesAsync(formulario, dados.ComunidadeId, cancellationToken);

        return View(formulario);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, DizimistaFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        var atual = await dizimistas.ObterParaEdicaoAsync(id, cancellationToken);
        if (atual is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var resultado = await dizimistas.AtualizarAsync(id, formulario.ParaDados(), cancellationToken);

            if (resultado.Erros.Any(erro => erro.Tipo == TipoErro.NaoEncontrado))
            {
                return NotFound();
            }

            if (resultado.Sucesso)
            {
                TempData[MensagemTempData.Sucesso] = "Dizimista atualizado com sucesso.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            AdicionarErros(resultado);
        }

        // A comunidade atual (mesmo inativa) continua disponível para manter o vínculo existente.
        await CarregarComunidadesAsync(formulario, atual.ComunidadeId, cancellationToken);
        return View(formulario);
    }

    private async Task CarregarComunidadesAsync(
        DizimistaFormularioViewModel formulario, int? comunidadeAtualId, CancellationToken cancellationToken)
    {
        var disponiveis = await comunidades.ListarParaVinculoAsync(comunidadeAtualId, cancellationToken);

        formulario.Comunidades = disponiveis
            .Select(comunidade => new SelectListItem(
                comunidade.Ativa ? comunidade.Nome : $"{comunidade.Nome} (inativa)",
                comunidade.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToList();
    }

    private void AdicionarErros(Resultado resultado)
    {
        foreach (var erro in resultado.Erros)
        {
            ModelState.AddModelError(erro.Campo, erro.Mensagem);
        }
    }
}
