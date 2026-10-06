using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Aplicacao.Services.Implementation;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.Repositories.Interface;

namespace SaoDimas.Tests.Aplicacao;

public sealed class EnvelopeDizimoAplicacaoTests
{
    private static readonly DizimistaIdentificacaoDto Joao = new(1, "000001", "João da Silva", "Paróquia São Dimas");
    private static readonly DizimistaIdentificacaoDto Maria = new(2, "000002", "Maria Souza", "Capela Santa Teresinha");
    private static readonly DizimistaIdentificacaoDto Conceicao = new(3, "000003", "Conceição Gonçalves", "Capela Santa Teresinha");

    private readonly PdfFake _pdf = new();

    private EnvelopeDizimoAplicacao CriarAplicacao() => new(
        new DizimistasFake([Joao, Maria, Conceicao], ativosPorComunidade: new() { [2] = [Conceicao, Maria] }),
        new ComunidadesFake(Criar.Comunidade(1, TipoComunidade.Matriz, nome: "Paróquia São Dimas"), Criar.Comunidade(2, nome: "Santa Teresinha"), Criar.Comunidade(3, nome: "Santo Inácio")),
        _pdf);

    [Fact]
    public async Task Todos_ativos_gera_um_documento_com_todas_as_comunidades()
    {
        var resultado = await CriarAplicacao().GerarTodosAtivosAsync(CancellationToken.None);
        Assert.True(resultado.Sucesso);
        Assert.Equal([Conceicao, Maria], Assert.Single(_pdf.Chamadas));
        Assert.Equal("envelopes-dizimo-todos-ativos-2.pdf", resultado.Valor.NomeArquivo);
    }

    [Fact]
    public async Task Todos_ativos_inclui_mais_de_500_sem_truncar()
    {
        var ativos = Enumerable.Range(1, 522).Select(i => new DizimistaIdentificacaoDto(i, i.ToString(System.Globalization.CultureInfo.InvariantCulture), $"Pessoa {i:D4}", "Capela teste")).ToArray();
        var app = new EnvelopeDizimoAplicacao(new DizimistasFake(ativos, new() { [1] = ativos }), new ComunidadesFake(), _pdf);
        var resultado = await app.GerarTodosAtivosAsync(CancellationToken.None);
        Assert.True(resultado.Sucesso);
        Assert.Equal(522, Assert.Single(_pdf.Chamadas).Count);
    }

    [Fact]
    public async Task Todos_ativos_sem_cadastros_nao_gera_documento_vazio()
    {
        var app = new EnvelopeDizimoAplicacao(new DizimistasFake([], new()), new ComunidadesFake(), _pdf);
        Assert.False((await app.GerarTodosAtivosAsync(CancellationToken.None)).Sucesso);
        Assert.Empty(_pdf.Chamadas);
    }

    [Fact]
    public async Task Envelope_individual_usa_os_dados_do_backend()
    {
        var resultado = await CriarAplicacao().GerarAsync(3, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(Conceicao, Assert.Single(Assert.Single(_pdf.Chamadas)));
        Assert.Equal("envelope-dizimo-000003-conceicao-goncalves.pdf", resultado.Valor.NomeArquivo);
    }

    [Fact]
    public async Task Dizimista_inexistente_retorna_nao_encontrado()
    {
        var resultado = await CriarAplicacao().GerarAsync(999, CancellationToken.None);

        Assert.Equal(TipoErro.NaoEncontrado, Assert.Single(resultado.Erros).Tipo);
        Assert.Empty(_pdf.Chamadas);
    }

    [Fact]
    public async Task Lote_gera_um_envelope_por_dizimista_sem_repetir()
    {
        var resultado = await CriarAplicacao().GerarEmLoteAsync([2, 1, 2, 3], CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal([Conceicao, Joao, Maria], Assert.Single(_pdf.Chamadas));
        Assert.Equal("envelopes-dizimo-3.pdf", resultado.Valor.NomeArquivo);
    }

    [Fact]
    public async Task Lote_com_dizimista_inexistente_e_rejeitado_por_inteiro()
    {
        var resultado = await CriarAplicacao().GerarEmLoteAsync([1, 999], CancellationToken.None);

        Assert.Equal(TipoErro.NaoEncontrado, Assert.Single(resultado.Erros).Tipo);
        Assert.Empty(_pdf.Chamadas);
    }

    [Fact]
    public async Task Lote_vazio_ou_acima_do_limite_e_rejeitado()
    {
        var aplicacao = CriarAplicacao();

        var vazio = await aplicacao.GerarEmLoteAsync([], CancellationToken.None);
        var excedido = await aplicacao.GerarEmLoteAsync(
            Enumerable.Range(1, IEnvelopeDizimoAplicacao.LimitePorDocumento + 1).ToArray(), CancellationToken.None);

        Assert.False(vazio.Sucesso);
        Assert.False(excedido.Sucesso);
        Assert.Empty(_pdf.Chamadas);
    }

    [Fact]
    public async Task Comunidade_gera_envelopes_dos_ativos()
    {
        var resultado = await CriarAplicacao().GerarParaComunidadeAsync(2, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal([Conceicao, Maria], Assert.Single(_pdf.Chamadas));
        Assert.Equal("envelopes-dizimo-capela-santa-teresinha.pdf", resultado.Valor.NomeArquivo);
    }

    [Fact]
    public async Task Comunidade_inexistente_ou_sem_ativos_e_rejeitada()
    {
        var aplicacao = CriarAplicacao();

        var inexistente = await aplicacao.GerarParaComunidadeAsync(99, CancellationToken.None);
        var semAtivos = await aplicacao.GerarParaComunidadeAsync(3, CancellationToken.None);

        Assert.Equal(TipoErro.NaoEncontrado, Assert.Single(inexistente.Erros).Tipo);
        Assert.Contains("Capela Santo Inácio", Assert.Single(semAtivos.Erros).Mensagem, StringComparison.Ordinal);
        Assert.Empty(_pdf.Chamadas);
    }

    private sealed class DizimistasFake(
        DizimistaIdentificacaoDto[] dizimistas,
        Dictionary<int, DizimistaIdentificacaoDto[]> ativosPorComunidade) : IDizimistaConsultas
    {
        public Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DizimistaIdentificacaoDto>>(
                dizimistas.Where(dizimista => ids.Contains(dizimista.Id)).OrderBy(dizimista => dizimista.Nome, StringComparer.Ordinal).ToList());

        public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAtivosAsync(int? comunidadeId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DizimistaIdentificacaoDto>>(comunidadeId.HasValue ? ativosPorComunidade.GetValueOrDefault(comunidadeId.Value, []) : ativosPorComunidade.Values.SelectMany(d => d).OrderBy(d => d.Nome, StringComparer.Ordinal).ToArray());
    }

    private sealed class ComunidadesFake(params Comunidade[] comunidades) : IComunidadeRepositorio
    {
        public Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(comunidades.FirstOrDefault(comunidade => comunidade.Id == id));

        public Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Comunidade>>(comunidades);
    }

    private sealed class PdfFake : IEnvelopeDizimoPdfService
    {
        public List<IReadOnlyList<DizimistaIdentificacaoDto>> Chamadas { get; } = [];

        public byte[] Gerar(IReadOnlyList<DizimistaIdentificacaoDto> envelopes)
        {
            Chamadas.Add(envelopes);
            return [0x25, 0x50, 0x44, 0x46];
        }
    }
}
