using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.MVC.Models;

namespace SaoDimas.MVC.Controllers;

/// <summary>
/// Envelope de Dízimo (molde A4). Somente leitura: recebe identificadores e os dados impressos
/// são sempre consultados no backend.
/// </summary>
[Route("Dizimistas")]
public sealed class EnvelopesController(
    IDizimistaAplicacao dizimistas,
    IEnvelopeDizimoAplicacao envelopes) : Controller
{
    [HttpGet("Envelopes/TodosAtivos")]
    public async Task<IActionResult> TodosAtivos(CancellationToken cancellationToken)
    {
        var resultado = await envelopes.GerarTodosAtivosAsync(cancellationToken);
        if (!resultado.Sucesso)
        {
            TempData[MensagemTempData.Erro] = string.Join(" ", resultado.Erros.Select(e => e.Mensagem));
            return RedirectToAction("Index", "Dizimistas");
        }
        return Arquivo(resultado, baixar: true);
    }

    [HttpGet("{dizimistaId:int}/Envelope")]
    public async Task<IActionResult> Preparar(int dizimistaId, CancellationToken cancellationToken)
    {
        var dizimista = await dizimistas.ObterDetalhesAsync(dizimistaId, cancellationToken);

        return dizimista is null ? NotFound() : View(dizimista);
    }

    /// <param name="baixar">Verdadeiro: baixa o arquivo. Falso: abre no visualizador do navegador (pré-visualização/impressão).</param>
    [HttpGet("{dizimistaId:int}/Envelope/Pdf")]
    public async Task<IActionResult> Pdf(int dizimistaId, bool baixar, CancellationToken cancellationToken) =>
        Arquivo(await envelopes.GerarAsync(dizimistaId, cancellationToken), baixar);

    /// <summary>
    /// Envelopes em lote: dos dizimistas selecionados (ids) ou de todos os ativos de uma comunidade.
    /// </summary>
    [HttpGet("Envelopes")]
    public async Task<IActionResult> Lote(
        [FromQuery] int[] ids, int? comunidadeId, bool baixar, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var resultado = comunidadeId is int comunidade
            ? await envelopes.GerarParaComunidadeAsync(comunidade, cancellationToken)
            : await envelopes.GerarEmLoteAsync(ids, cancellationToken);

        return Arquivo(resultado, baixar);
    }

    private IActionResult Arquivo(Resultado<ArquivoPdf> resultado, bool baixar)
    {
        if (!resultado.Sucesso)
        {
            return resultado.Erros.Any(erro => erro.Tipo == TipoErro.NaoEncontrado)
                ? NotFound()
                : BadRequest(string.Join(" ", resultado.Erros.Select(erro => erro.Mensagem)));
        }

        var arquivo = resultado.Valor;
        if (baixar)
        {
            return File(arquivo.Conteudo, ArquivoPdf.TipoConteudo, arquivo.NomeArquivo);
        }

        Response.Headers.ContentDisposition = new ContentDisposition { Inline = true, FileName = arquivo.NomeArquivo }.ToString();
        return File(arquivo.Conteudo, ArquivoPdf.TipoConteudo);
    }
}
