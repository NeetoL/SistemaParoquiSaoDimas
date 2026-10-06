using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;

namespace SaoDimas.MVC.Controllers;

/// <summary>
/// Lotes de tickets, distribuição por faixas, prestação de contas e impressão.
/// </summary>
[Route("Eventos/{eventoId:int}")]
public sealed class TicketsController(ITicketAplicacao tickets) : ModuloEventosController
{
    [HttpPost("Produtos/{produtoId:int}/Lotes/Formulario")]
    public async Task<IActionResult> FormularioLote(int eventoId, int produtoId, CancellationToken cancellationToken)
    {
        var preparacao = await tickets.ObterPreparacaoLoteAsync(eventoId, produtoId, cancellationToken);
        if (preparacao is null)
        {
            return NotFound("Produto não encontrado.");
        }

        var formulario = new LoteFormularioViewModel { Preparacao = preparacao };
        formulario.Simulacao = await tickets.SimularLoteAsync(eventoId, produtoId, formulario.Quantidade!.Value, cancellationToken);
        return Modal("_FormularioLote", formulario);
    }

    /// <summary>Prévia (faixa e valor potencial) calculada no servidor enquanto a quantidade é digitada.</summary>
    [HttpPost("Produtos/{produtoId:int}/Lotes/Simular")]
    public async Task<IActionResult> SimularLote(int eventoId, int produtoId, int? quantidade, CancellationToken cancellationToken) =>
        PartialView("_SimulacaoLote", await tickets.SimularLoteAsync(eventoId, produtoId, quantidade ?? 0, cancellationToken));

    [HttpPost("Produtos/{produtoId:int}/Lotes")]
    public async Task<IActionResult> GerarLote(int eventoId, int produtoId, LoteFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (ModelState.IsValid)
        {
            var resultado = await tickets.GerarLoteAsync(eventoId, produtoId, formulario.Quantidade!.Value, cancellationToken);
            if (resultado.Sucesso)
            {
                return Redirecionar(Url.Action(nameof(Lote), new { eventoId, loteId = resultado.Valor })!, "Lote de tickets gerado.");
            }

            AdicionarErros(resultado);
        }

        var preparacao = await tickets.ObterPreparacaoLoteAsync(eventoId, produtoId, cancellationToken);
        if (preparacao is null)
        {
            return NotFound("Produto não encontrado.");
        }

        formulario.Preparacao = preparacao;
        formulario.Simulacao = await tickets.SimularLoteAsync(eventoId, produtoId, formulario.Quantidade ?? 0, cancellationToken);
        return PartialView("_FormularioLote", formulario);
    }

    [HttpGet("Lotes/{loteId:int}")]
    public IActionResult Lote(int eventoId, int loteId) => View((eventoId, loteId));

    [HttpPost("Lotes/{loteId:int}/Conteudo")]
    public async Task<IActionResult> ConteudoLote(int eventoId, int loteId, CancellationToken cancellationToken)
    {
        var lote = await tickets.ObterLoteAsync(eventoId, loteId, cancellationToken);
        return lote is null ? PartialView("~/Views/Eventos/_NaoEncontrado.cshtml") : PartialView("_Lote", lote);
    }

    /// <param name="numeroInicial">Pré-preenchimento ao distribuir uma lacuna.</param>
    [HttpPost("Lotes/{loteId:int}/Distribuicoes/Formulario")]
    public async Task<IActionResult> FormularioDistribuicao(
        int eventoId, int loteId, int? distribuicaoId, int? numeroInicial, int? numeroFinal, CancellationToken cancellationToken)
    {
        var lote = await tickets.ObterLoteAsync(eventoId, loteId, cancellationToken);
        if (lote is null)
        {
            return NotFound("Lote não encontrado.");
        }

        var formulario = new DistribuicaoFormularioViewModel { Lote = lote, NumeroInicial = numeroInicial, NumeroFinal = numeroFinal };
        if (distribuicaoId is int id)
        {
            var atual = await tickets.ObterDistribuicaoAsync(eventoId, loteId, id, cancellationToken);
            if (atual is null)
            {
                return NotFound("Faixa não encontrada.");
            }

            (formulario.DistribuicaoId, formulario.NumeroInicial, formulario.NumeroFinal, formulario.Responsavel) =
                (id, atual.NumeroInicial, atual.NumeroFinal, atual.Responsavel);
        }

        return Modal("_FormularioDistribuicao", formulario);
    }

    [HttpPost("Lotes/{loteId:int}/Distribuicoes")]
    public async Task<IActionResult> SalvarDistribuicao(
        int eventoId, int loteId, DistribuicaoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (ModelState.IsValid)
        {
            var dados = new DadosDistribuicao(formulario.NumeroInicial!.Value, formulario.NumeroFinal!.Value, formulario.Responsavel,
                User.Identity?.IsAuthenticated == true ? User.Identity.Name : formulario.Operador);
            var resultado = formulario.DistribuicaoId is int id
                ? await tickets.AlterarDistribuicaoAsync(eventoId, loteId, id, dados, cancellationToken)
                : await tickets.DistribuirAsync(eventoId, loteId, dados, cancellationToken);

            if (resultado.Sucesso)
            {
                return Concluido(formulario.DistribuicaoId is null ? "Tickets distribuídos." : "Faixa atualizada.");
            }

            AdicionarErros(resultado);
        }

        var lote = await tickets.ObterLoteAsync(eventoId, loteId, cancellationToken);
        if (lote is null)
        {
            return NotFound("Lote não encontrado.");
        }

        formulario.Lote = lote;
        return PartialView("_FormularioDistribuicao", formulario);
    }

    [HttpPost("Lotes/{loteId:int}/Distribuicoes/{distribuicaoId:int}/Remover")]
    public async Task<IActionResult> RemoverDistribuicao(int eventoId, int loteId, int distribuicaoId, string? operador, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operador)) return BadRequest("Informe o operador das ações do lote.");
        var resultado = await tickets.RemoverDistribuicaoAsync(eventoId, loteId, distribuicaoId, cancellationToken,
            User.Identity?.IsAuthenticated == true ? User.Identity.Name : operador.Trim());
        return resultado.Sucesso ? Concluido("Faixa cancelada: os tickets voltaram a ficar disponíveis.") : NaoEncontradoOuErro(resultado);
    }

    [HttpPost("Lotes/{loteId:int}/Distribuicoes/{distribuicaoId:int}/Prestacao/Formulario")]
    public async Task<IActionResult> FormularioPrestacao(int eventoId, int loteId, int distribuicaoId, CancellationToken cancellationToken)
    {
        var preparacao = await tickets.ObterPrestacaoAsync(eventoId, loteId, distribuicaoId, cancellationToken);
        if (preparacao is null)
        {
            return NotFound("Faixa não encontrada.");
        }

        return Redirecionar($"/Eventos/{eventoId}#prestacao", "Utilize a central de prestação de contas da edição.");
    }

    /// <summary>Valor esperado e diferença calculados no servidor enquanto os campos são preenchidos.</summary>
    [HttpPost("Lotes/{loteId:int}/Distribuicoes/{distribuicaoId:int}/Prestacao/Simular")]
    public async Task<IActionResult> SimularPrestacao(
        int eventoId, int loteId, int distribuicaoId, PrestacaoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        var preparacao = await tickets.ObterPrestacaoAsync(eventoId, loteId, distribuicaoId, cancellationToken);
        if (preparacao is null)
        {
            return NotFound("Faixa não encontrada.");
        }

        formulario.Preparacao = preparacao;
        formulario.Simulacao = await SimularAsync(preparacao, formulario, cancellationToken);
        return PartialView("_SimulacaoPrestacao", formulario);
    }

    [HttpPost("Lotes/{loteId:int}/Distribuicoes/{distribuicaoId:int}/Prestacao")]
    public async Task<IActionResult> RegistrarPrestacao(
        int eventoId, int loteId, int distribuicaoId, PrestacaoFormularioViewModel formulario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ValorMonetario.TentarLer(formulario.ValorEntregue, out var valorEntregue))
        {
            ModelState.AddModelError(nameof(formulario.ValorEntregue), "Informe um valor válido. Ex.: 450,00");
        }

        if (ModelState.IsValid)
        {
            var resultado = await tickets.RegistrarPrestacaoAsync(eventoId, loteId, distribuicaoId,
                new DadosPrestacao(formulario.Vendidos!.Value, formulario.Devolvidos!.Value, valorEntregue, formulario.Justificativa),
                cancellationToken);

            if (resultado.Sucesso)
            {
                return Concluido("Prestação de contas registrada.");
            }

            AdicionarErros(resultado);
        }

        var preparacao = await tickets.ObterPrestacaoAsync(eventoId, loteId, distribuicaoId, cancellationToken);
        if (preparacao is null)
        {
            return NotFound("Faixa não encontrada.");
        }

        formulario.Preparacao = preparacao;
        formulario.Simulacao = await SimularAsync(preparacao, formulario, cancellationToken);
        return PartialView("_FormularioPrestacao", formulario);
    }

    /// <summary>
    /// PDF A4 dos tickets do lote ou de uma faixa. Formulário comum (abre em nova aba) com os dados do servidor.
    /// </summary>
    [HttpPost("Lotes/{loteId:int}/Imprimir")]
    public async Task<IActionResult> Imprimir(int eventoId, int loteId, int? distribuicaoId, CancellationToken cancellationToken)
    {
        var resultado = await tickets.GerarPdfAsync(eventoId, loteId, distribuicaoId, cancellationToken);
        if (!resultado.Sucesso)
        {
            return NaoEncontradoOuErro(resultado);
        }

        Response.Headers.ContentDisposition = new ContentDisposition { Inline = true, FileName = resultado.Valor.NomeArquivo }.ToString();
        return File(resultado.Valor.Conteudo, ArquivoPdf.TipoConteudo);
    }

    private Task<SimulacaoPrestacaoDto?> SimularAsync(
        PreparacaoPrestacaoDto preparacao, PrestacaoFormularioViewModel formulario, CancellationToken cancellationToken) =>
        tickets.SimularPrestacaoAsync(
            preparacao.EventoId, preparacao.LoteId, preparacao.DistribuicaoId, formulario.Vendidos ?? 0,
            ValorMonetario.TentarLer(formulario.ValorEntregue, out var valor) ? valor : 0, cancellationToken);
}
