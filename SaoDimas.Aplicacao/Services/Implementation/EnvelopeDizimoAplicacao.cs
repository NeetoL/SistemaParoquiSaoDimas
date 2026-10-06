using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Repositories.Interface;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class EnvelopeDizimoAplicacao(
    IDizimistaConsultas dizimistas,
    IComunidadeRepositorio comunidades,
    IEnvelopeDizimoPdfService pdf) : IEnvelopeDizimoAplicacao
{
    public async Task<Resultado<ArquivoPdf>> GerarTodosAtivosAsync(CancellationToken cancellationToken)
    {
        var ativos = await dizimistas.ListarIdentificacoesAtivosAsync(null, cancellationToken);
        if (ativos.Count == 0)
            return Resultado<ArquivoPdf>.Falha(new Erro(string.Empty, "Não há dizimistas ativos para gerar envelopes."));

        cancellationToken.ThrowIfCancellationRequested();
        return Resultado<ArquivoPdf>.Ok(new ArquivoPdf(pdf.Gerar(ativos), $"envelopes-dizimo-todos-ativos-{ativos.Count}.pdf"));
    }

    public async Task<Resultado<ArquivoPdf>> GerarAsync(int dizimistaId, CancellationToken cancellationToken)
    {
        var encontrados = await dizimistas.ListarIdentificacoesAsync([dizimistaId], cancellationToken);
        if (encontrados.Count == 0)
        {
            return Resultado<ArquivoPdf>.Falha(Erro.NaoEncontrado("Dizimista não encontrado."));
        }

        var dizimista = encontrados[0];
        return Resultado<ArquivoPdf>.Ok(new ArquivoPdf(
            pdf.Gerar(encontrados), $"envelope-dizimo-{dizimista.Codigo}-{Slug(dizimista.Nome)}.pdf"));
    }

    public async Task<Resultado<ArquivoPdf>> GerarEmLoteAsync(IReadOnlyCollection<int> dizimistaIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dizimistaIds);

        var ids = dizimistaIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return Resultado<ArquivoPdf>.Falha(new Erro(string.Empty, "Selecione ao menos um dizimista."));
        }

        if (ids.Count > IEnvelopeDizimoAplicacao.LimitePorDocumento)
        {
            return Resultado<ArquivoPdf>.Falha(LimiteExcedido());
        }

        var encontrados = await dizimistas.ListarIdentificacoesAsync(ids, cancellationToken);
        if (encontrados.Count != ids.Count)
        {
            return Resultado<ArquivoPdf>.Falha(Erro.NaoEncontrado("Um ou mais dizimistas selecionados não foram encontrados."));
        }

        return Resultado<ArquivoPdf>.Ok(new ArquivoPdf(pdf.Gerar(encontrados), $"envelopes-dizimo-{encontrados.Count}.pdf"));
    }

    public async Task<Resultado<ArquivoPdf>> GerarParaComunidadeAsync(int comunidadeId, CancellationToken cancellationToken)
    {
        var comunidade = await comunidades.ObterPorIdAsync(comunidadeId, cancellationToken);
        if (comunidade is null)
        {
            return Resultado<ArquivoPdf>.Falha(Erro.NaoEncontrado("Comunidade não encontrada."));
        }

        var ativos = await dizimistas.ListarIdentificacoesAtivosAsync(comunidadeId, cancellationToken);
        if (ativos.Count == 0)
        {
            return Resultado<ArquivoPdf>.Falha(new Erro(string.Empty, $"Não há dizimistas ativos em {comunidade.NomeCompleto}."));
        }

        if (ativos.Count > IEnvelopeDizimoAplicacao.LimitePorDocumento)
        {
            return Resultado<ArquivoPdf>.Falha(LimiteExcedido());
        }

        return Resultado<ArquivoPdf>.Ok(new ArquivoPdf(pdf.Gerar(ativos), $"envelopes-dizimo-{Slug(comunidade.NomeCompleto)}.pdf"));
    }

    private static Erro LimiteExcedido() =>
        new(string.Empty, $"Gere no máximo {IEnvelopeDizimoAplicacao.LimitePorDocumento} envelopes por vez.");

    /// <summary>
    /// Texto em minúsculas, sem acentos e somente ASCII, seguro para nome de arquivo.
    /// </summary>
    private static string Slug(string texto)
    {
        var semAcentos = new string(texto
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return string.Join('-', Regex.Split(semAcentos.ToLowerInvariant(), "[^a-z0-9]+").Where(parte => parte.Length > 0));
    }
}
