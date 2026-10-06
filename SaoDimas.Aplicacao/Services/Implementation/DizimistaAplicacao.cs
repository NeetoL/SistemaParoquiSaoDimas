using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Repositories.Interface;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class DizimistaAplicacao(
    IDizimistaRepositorio dizimistas,
    IComunidadeRepositorio comunidades,
    IDizimistaConsultas consultas,
    TimeProvider relogio) : IDizimistaAplicacao
{
    private static readonly Erro DizimistaNaoEncontrado = Erro.NaoEncontrado("Dizimista não encontrado.");

    public Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        return consultas.PesquisarAsync(filtro.Normalizado(), cancellationToken);
    }

    public Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken) =>
        consultas.ObterDetalhesAsync(id, cancellationToken);

    public async Task<DadosDizimista?> ObterParaEdicaoAsync(int id, CancellationToken cancellationToken)
    {
        var dizimista = await dizimistas.ObterPorIdAsync(id, cancellationToken);

        return dizimista is null
            ? null
            : new DadosDizimista(
                dizimista.Nome,
                dizimista.Cpf?.Formatado,
                dizimista.Telefone?.Formatado,
                dizimista.ComunidadeId,
                dizimista.DataEntrada,
                dizimista.Status, dizimista.Endereco, dizimista.Cep, dizimista.Bairro, dizimista.DataNascimento);
    }

    public async Task<Resultado<int>> CadastrarAsync(DadosDizimista dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var comunidade = await comunidades.ObterPorIdAsync(dados.ComunidadeId, cancellationToken);
        if (comunidade is null)
        {
            return Resultado<int>.Falha(ComunidadeInvalida());
        }

        var erroCpf = await ValidarCpfUnicoAsync(dados.Cpf, ignorarDizimistaId: null, cancellationToken);
        if (erroCpf is not null)
        {
            return Resultado<int>.Falha(erroCpf);
        }

        var resultado = Dizimista.Criar(
            dados.Nome, dados.Cpf, dados.Telefone, comunidade, dados.DataEntrada, dados.Status, relogio.GetLocalNow(), dados.Endereco, dados.Cep, dados.Bairro, dados.DataNascimento);

        if (!resultado.Sucesso)
        {
            return Resultado<int>.Falha(resultado.Erros);
        }

        dizimistas.Adicionar(resultado.Valor);
        await dizimistas.SalvarAlteracoesAsync(cancellationToken);

        return Resultado<int>.Ok(resultado.Valor.Id);
    }

    public async Task<Resultado> AtualizarAsync(int id, DadosDizimista dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var dizimista = await dizimistas.ObterPorIdAsync(id, cancellationToken);
        if (dizimista is null)
        {
            return Resultado.Falha(DizimistaNaoEncontrado);
        }

        var comunidade = await comunidades.ObterPorIdAsync(dados.ComunidadeId, cancellationToken);
        if (comunidade is null)
        {
            return Resultado.Falha(ComunidadeInvalida());
        }

        var erroCpf = await ValidarCpfUnicoAsync(dados.Cpf, ignorarDizimistaId: id, cancellationToken);
        if (erroCpf is not null)
        {
            return Resultado.Falha(erroCpf);
        }

        var resultado = dizimista.Atualizar(
            dados.Nome, dados.Cpf, dados.Telefone, comunidade, dados.DataEntrada, dados.Status, relogio.GetLocalNow(), dados.Endereco, dados.Cep, dados.Bairro, dados.DataNascimento);

        if (resultado.Sucesso)
        {
            await dizimistas.SalvarAlteracoesAsync(cancellationToken);
        }

        return resultado;
    }

    private static Erro ComunidadeInvalida() =>
        new(nameof(DadosDizimista.ComunidadeId), "Selecione uma comunidade válida.");

    /// <summary>
    /// CPF inválido é tratado pela entidade; aqui só se verifica duplicidade de um CPF válido.
    /// </summary>
    private async Task<Erro?> ValidarCpfUnicoAsync(string? cpf, int? ignorarDizimistaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return null;
        }

        var resultado = Cpf.Criar(cpf);
        if (!resultado.Sucesso)
        {
            return null;
        }

        return await dizimistas.ExisteCpfAsync(resultado.Valor, ignorarDizimistaId, cancellationToken)
            ? new Erro(nameof(DadosDizimista.Cpf), "Já existe um dizimista cadastrado com este CPF.")
            : null;
    }
}
