using Microsoft.EntityFrameworkCore;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.ValueObjects;
using SaoDimas.Infraestrutura.Persistencia;

namespace SaoDimas.Infraestrutura.Queries.Implementation;

/// <summary>
/// Leitura de dizimistas com projeção: filtros, ordenação e paginação executados no SQL Server,
/// em uma única consulta com JOIN para a comunidade (sem N+1).
/// </summary>
internal sealed class DizimistaConsultas(SaoDimasDbContext contexto) : IDizimistaConsultas
{
    public async Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = Filtrar(contexto.Dizimistas.AsNoTracking(), filtro);

        var total = await consulta.CountAsync(cancellationToken);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanhoPagina));
        var pagina = Math.Min(filtro.Pagina, totalPaginas);

        var linhas = await Ordenar(ComComunidade(consulta), filtro)
            .Skip((pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .Select(linha => new
            {
                linha.Dizimista.Id,
                linha.Dizimista.CodigoOriginal,
                linha.Dizimista.Nome,
                linha.Dizimista.Telefone,
                linha.Dizimista.Status,
                linha.ComunidadeNome,
                linha.ComunidadeTipo
            })
            .ToListAsync(cancellationToken);

        var itens = linhas
            .Select(linha => new DizimistaResumoDto(
                linha.Id,
                linha.CodigoOriginal ?? Dizimista.FormatarCodigo(linha.Id),
                linha.Nome,
                Comunidade.FormatarNome(linha.ComunidadeTipo, linha.ComunidadeNome),
                linha.Telefone?.Formatado,
                linha.Status))
            .ToList();

        return new PaginaResultado<DizimistaResumoDto>(itens, pagina, filtro.TamanhoPagina, total);
    }

    public async Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken cancellationToken)
    {
        var linha = await ComComunidade(contexto.Dizimistas.AsNoTracking().Where(dizimista => dizimista.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        if (linha is null)
        {
            return null;
        }

        var dizimista = linha.Dizimista;

        return new DizimistaDetalhesDto(
            dizimista.Id,
            dizimista.Codigo,
            dizimista.Nome,
            dizimista.Cpf?.Formatado,
            dizimista.Telefone?.Formatado,
            Comunidade.FormatarNome(linha.ComunidadeTipo, linha.ComunidadeNome),
            dizimista.DataEntrada,
            dizimista.Status,
            dizimista.CriadoEm,
            dizimista.AtualizadoEm, dizimista.Endereco, dizimista.Cep, dizimista.Bairro, dizimista.DataNascimento);
    }

    public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return ListarIdentificacoesAsync(
            contexto.Dizimistas.AsNoTracking().Where(dizimista => ids.Contains(dizimista.Id)), cancellationToken);
    }

    public Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAtivosDaComunidadeAsync(
        int comunidadeId, CancellationToken cancellationToken) =>
        ListarIdentificacoesAsync(
            contexto.Dizimistas.AsNoTracking()
                .Where(dizimista => dizimista.ComunidadeId == comunidadeId && dizimista.Status == StatusDizimista.Ativo),
            cancellationToken);

    /// <summary>
    /// Projeção para o envelope, sem CPF, em uma única consulta com JOIN.
    /// </summary>
    private async Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(
        IQueryable<Dizimista> consulta, CancellationToken cancellationToken)
    {
        var linhas = await ComComunidade(consulta)
            .OrderBy(linha => linha.Dizimista.Nome)
            .ThenBy(linha => linha.Dizimista.Id)
            .Select(linha => new { linha.Dizimista.Id, linha.Dizimista.CodigoOriginal, linha.Dizimista.Nome, linha.Dizimista.Telefone, linha.Dizimista.Endereco, linha.Dizimista.Cep, linha.Dizimista.Bairro, linha.Dizimista.DataNascimento, linha.ComunidadeNome, linha.ComunidadeTipo })
            .ToListAsync(cancellationToken);

        return linhas
            .Select(linha => new DizimistaIdentificacaoDto(
                linha.Id,
                linha.CodigoOriginal ?? Dizimista.FormatarCodigo(linha.Id),
                linha.Nome,
                Comunidade.FormatarNome(linha.ComunidadeTipo, linha.ComunidadeNome), linha.Telefone?.Formatado, linha.Endereco, linha.Cep, linha.Bairro, linha.DataNascimento))
            .ToList();
    }

    private sealed class DizimistaComComunidade
    {
        public required Dizimista Dizimista { get; init; }
        public required string ComunidadeNome { get; init; }
        public required TipoComunidade ComunidadeTipo { get; init; }
        public required int ComunidadeOrdem { get; init; }
    }

    private IQueryable<DizimistaComComunidade> ComComunidade(IQueryable<Dizimista> dizimistas) =>
        dizimistas.Join(
            contexto.Comunidades,
            dizimista => dizimista.ComunidadeId,
            comunidade => comunidade.Id,
            (dizimista, comunidade) => new DizimistaComComunidade
            {
                Dizimista = dizimista,
                ComunidadeNome = comunidade.Nome,
                ComunidadeTipo = comunidade.Tipo,
                ComunidadeOrdem = comunidade.OrdemExibicao
            });

    /// <summary>
    /// A busca aceita nome (parcial, sem diferenciar acentos), código, telefone ou CPF completos.
    /// </summary>
    private static IQueryable<Dizimista> Filtrar(IQueryable<Dizimista> consulta, FiltroDizimistas filtro)
    {
        if (filtro.ComunidadeId is int comunidadeId)
        {
            consulta = consulta.Where(dizimista => dizimista.ComunidadeId == comunidadeId);
        }

        if (filtro.Busca is not { } busca)
        {
            return consulta;
        }

        var digitos = new string(busca.Where(char.IsAsciiDigit).ToArray());
        var buscaSoComDigitos = digitos.Length > 0 && busca.All(c => char.IsAsciiDigit(c) || c is '.' or '-' or '(' or ')' or ' ');

        int? codigo = buscaSoComDigitos && digitos.Length <= 9 ? int.Parse(digitos, System.Globalization.CultureInfo.InvariantCulture) : null;
        var telefone = buscaSoComDigitos && Telefone.Criar(digitos) is { Sucesso: true } resultadoTelefone ? resultadoTelefone.Valor : null;
        var cpf = buscaSoComDigitos && Cpf.Criar(digitos) is { Sucesso: true } resultadoCpf ? resultadoCpf.Valor : null;

        return consulta.Where(dizimista =>
            dizimista.Nome.Contains(busca)
            || dizimista.CodigoOriginal == busca
            || (codigo != null && dizimista.CodigoOriginal == digitos)
            || (codigo != null && dizimista.CodigoOriginal == null && dizimista.Id == codigo)
            || (telefone != null && dizimista.Telefone == telefone)
            || (cpf != null && dizimista.Cpf == cpf));
    }

    private static IQueryable<DizimistaComComunidade> Ordenar(IQueryable<DizimistaComComunidade> consulta, FiltroDizimistas filtro) =>
        (filtro.Ordenacao, filtro.Descendente) switch
        {
            (OrdenacaoDizimistas.Codigo, false) => consulta.OrderBy(l => l.Dizimista.Id),
            (OrdenacaoDizimistas.Codigo, true) => consulta.OrderByDescending(l => l.Dizimista.Id),
            (OrdenacaoDizimistas.Comunidade, false) => consulta.OrderBy(l => l.ComunidadeOrdem).ThenBy(l => l.Dizimista.Nome).ThenBy(l => l.Dizimista.Id),
            (OrdenacaoDizimistas.Comunidade, true) => consulta.OrderByDescending(l => l.ComunidadeOrdem).ThenBy(l => l.Dizimista.Nome).ThenBy(l => l.Dizimista.Id),
            (_, true) => consulta.OrderByDescending(l => l.Dizimista.Nome).ThenBy(l => l.Dizimista.Id),
            _ => consulta.OrderBy(l => l.Dizimista.Nome).ThenBy(l => l.Dizimista.Id)
        };
}
