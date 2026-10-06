using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Aplicacao.Services.Implementation;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Tests.Aplicacao;

public sealed class DizimistaAplicacaoTests
{
    private readonly ComunidadesEmMemoria _comunidades = new(
        Criar.Comunidade(1, TipoComunidade.Matriz),
        Criar.Comunidade(2),
        Criar.Comunidade(3, ativa: false));

    private readonly DizimistasEmMemoria _dizimistas = new();

    private DizimistaAplicacao CriarAplicacao() =>
        new(_dizimistas, _comunidades, new ConsultasNaoUsadas(), new RelogioFixo());

    private static DadosDizimista Dados(int comunidadeId, string? cpf = null) =>
        new("João da Silva", cpf, "(11) 98765-4321", comunidadeId, Criar.Hoje, StatusDizimista.Ativo);

    [Fact]
    public async Task Dados_do_envelope_sao_salvos_e_retornados_para_edicao()
    {
        var dados = Dados(2) with { Endereco = "Rua A, 20", Cep = "21775-280", Bairro = "Bangu", DataNascimento = new DateOnly(1990, 1, 2) };
        var app = CriarAplicacao();
        Assert.True((await app.CadastrarAsync(dados, CancellationToken.None)).Sucesso);
        var salvo = Assert.Single(_dizimistas.Adicionados);
        Assert.Equal("Rua A, 20", salvo.Endereco);
        Assert.Equal("21775280", salvo.Cep);
        _dizimistas.Existentes.Add(salvo);
        var edicao = await app.ObterParaEdicaoAsync(salvo.Id, CancellationToken.None);
        Assert.Equal(dados.Endereco, edicao!.Endereco);
        Assert.Equal(dados.DataNascimento, edicao.DataNascimento);
    }

    [Fact]
    public async Task Cadastra_e_persiste_dizimista_vinculado_a_comunidade()
    {
        var resultado = await CriarAplicacao().CadastrarAsync(Dados(comunidadeId: 2), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        var salvo = Assert.Single(_dizimistas.Adicionados);
        Assert.Equal(2, salvo.ComunidadeId);
        Assert.Equal(1, _dizimistas.Salvamentos);
    }

    [Fact]
    public async Task Rejeita_comunidade_inexistente_recebida_do_navegador()
    {
        var resultado = await CriarAplicacao().CadastrarAsync(Dados(comunidadeId: 999), CancellationToken.None);

        Assert.Equal(nameof(DadosDizimista.ComunidadeId), Assert.Single(resultado.Erros).Campo);
        Assert.Empty(_dizimistas.Adicionados);
    }

    [Fact]
    public async Task Rejeita_comunidade_inativa()
    {
        var resultado = await CriarAplicacao().CadastrarAsync(Dados(comunidadeId: 3), CancellationToken.None);

        Assert.Equal(nameof(DadosDizimista.ComunidadeId), Assert.Single(resultado.Erros).Campo);
        Assert.Equal(0, _dizimistas.Salvamentos);
    }

    [Fact]
    public async Task Rejeita_cpf_ja_cadastrado()
    {
        _dizimistas.Existentes.Add(Criar.Dizimista(10, _comunidades.Todas[0], cpf: "123.456.789-09"));

        var resultado = await CriarAplicacao().CadastrarAsync(Dados(comunidadeId: 2, cpf: "12345678909"), CancellationToken.None);

        Assert.Equal(nameof(DadosDizimista.Cpf), Assert.Single(resultado.Erros).Campo);
    }

    [Fact]
    public async Task Edicao_pode_manter_o_proprio_cpf_e_trocar_de_comunidade()
    {
        _dizimistas.Existentes.Add(Criar.Dizimista(10, _comunidades.Todas[0], cpf: "123.456.789-09"));

        var resultado = await CriarAplicacao().AtualizarAsync(10, Dados(comunidadeId: 2, cpf: "12345678909"), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, _dizimistas.Existentes[0].ComunidadeId);
        Assert.Equal(1, _dizimistas.Salvamentos);
    }

    [Fact]
    public async Task Edicao_de_dizimista_inexistente_retorna_nao_encontrado()
    {
        var resultado = await CriarAplicacao().AtualizarAsync(404, Dados(comunidadeId: 2), CancellationToken.None);

        Assert.Equal(TipoErro.NaoEncontrado, Assert.Single(resultado.Erros).Tipo);
    }

    private sealed class ComunidadesEmMemoria(params Comunidade[] comunidades) : IComunidadeRepositorio
    {
        public Comunidade[] Todas { get; } = comunidades;

        public Task<Comunidade?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Todas.FirstOrDefault(comunidade => comunidade.Id == id));

        public Task<IReadOnlyList<Comunidade>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Comunidade>>(Todas);
    }

    private sealed class DizimistasEmMemoria : IDizimistaRepositorio
    {
        public List<Dizimista> Existentes { get; } = [];

        public List<Dizimista> Adicionados { get; } = [];

        public int Salvamentos { get; private set; }

        public Task<Dizimista?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Existentes.FirstOrDefault(dizimista => dizimista.Id == id));

        public Task<bool> ExisteCpfAsync(Cpf cpf, int? ignorarDizimistaId, CancellationToken cancellationToken) =>
            Task.FromResult(Existentes.Any(dizimista => dizimista.Cpf == cpf && dizimista.Id != ignorarDizimistaId));

        public void Adicionar(Dizimista dizimista) => Adicionados.Add(dizimista);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }
    }

    private sealed class ConsultasNaoUsadas : IDizimistaConsultas
    {
        public Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAtivosDaComunidadeAsync(int comunidadeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
