using System.Globalization;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Queries.Interface;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Infraestrutura.Persistencia.Json;
namespace SaoDimas.Infraestrutura.Queries.Implementation;
internal sealed class DizimistaJsonConsultas(SessaoSistemaJson sessao) : IDizimistaConsultas
{
    private static readonly StringComparer Comparador = StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), ignoreCase: true);
    private Comunidade Comunidade(Dizimista d) => sessao.Comunidades.Single(c => c.Id == d.ComunidadeId);
    public async Task<PaginaResultado<DizimistaResumoDto>> PesquisarAsync(FiltroDizimistas filtro, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        filtro = filtro.Normalizado();
        IEnumerable<Dizimista> consulta = sessao.Dizimistas;
        if (filtro.ComunidadeId is int comunidade) consulta = consulta.Where(d => d.ComunidadeId == comunidade);
        if (filtro.Busca is { } busca)
        {
            var digitos = new string(busca.Where(char.IsAsciiDigit).ToArray());
            var numerica = digitos.Length > 0 && busca.All(c => char.IsAsciiDigit(c) || c is '.' or '-' or '(' or ')' or ' ');
            var codigo = numerica && int.TryParse(digitos, out var id) ? id : 0;
            consulta = consulta.Where(d => CultureInfo.GetCultureInfo("pt-BR").CompareInfo.IndexOf(d.Nome, busca, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0
                || (numerica && (d.Id == codigo || d.Telefone?.Valor == digitos || d.Cpf?.Valor == digitos)));
        }
        var total = consulta.Count();
        IOrderedEnumerable<Dizimista> ordenada = filtro.Ordenacao switch
        {
            OrdenacaoDizimistas.Codigo => filtro.Descendente ? consulta.OrderByDescending(d => d.Id) : consulta.OrderBy(d => d.Id),
            OrdenacaoDizimistas.Comunidade => filtro.Descendente ? consulta.OrderByDescending(d => Comunidade(d).OrdemExibicao).ThenBy(d => d.Nome, Comparador) : consulta.OrderBy(d => Comunidade(d).OrdemExibicao).ThenBy(d => d.Nome, Comparador),
            _ => filtro.Descendente ? consulta.OrderByDescending(d => d.Nome, Comparador) : consulta.OrderBy(d => d.Nome, Comparador)
        };
        var pagina = Math.Min(filtro.Pagina, Math.Max(1, (int)Math.Ceiling(total / (double)filtro.TamanhoPagina)));
        var itens = ordenada.ThenBy(d => d.Id).Skip((pagina - 1) * filtro.TamanhoPagina).Take(filtro.TamanhoPagina)
            .Select(d => new DizimistaResumoDto(d.Id, d.Codigo, d.Nome, Comunidade(d).NomeCompleto, d.Telefone?.Formatado, d.Status)).ToList();
        return new(itens, pagina, filtro.TamanhoPagina, total);
    }
    public async Task<DizimistaDetalhesDto?> ObterDetalhesAsync(int id, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        var d = sessao.Dizimistas.FirstOrDefault(d => d.Id == id);
        return d is null ? null : new(d.Id, d.Codigo, d.Nome, d.Cpf?.Formatado, d.Telefone?.Formatado, Comunidade(d).NomeCompleto,
            d.DataEntrada, d.Status, d.CriadoEm, d.AtualizadoEm, d.Endereco, d.Cep, d.Bairro, d.DataNascimento);
    }
    public async Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return Identificacoes(sessao.Dizimistas.Where(d => ids.Contains(d.Id)));
    }
    public async Task<IReadOnlyList<DizimistaIdentificacaoDto>> ListarIdentificacoesAtivosDaComunidadeAsync(int id, CancellationToken ct)
    {
        await sessao.CarregarAsync(ct);
        return Identificacoes(sessao.Dizimistas.Where(d => d.ComunidadeId == id && d.Status == StatusDizimista.Ativo));
    }
    private List<DizimistaIdentificacaoDto> Identificacoes(IEnumerable<Dizimista> consulta) => consulta.OrderBy(d => d.Nome, Comparador).ThenBy(d => d.Id)
        .Select(d => new DizimistaIdentificacaoDto(d.Id, d.Codigo, d.Nome, Comunidade(d).NomeCompleto, d.Telefone?.Formatado,
            d.Endereco, d.Cep, d.Bairro, d.DataNascimento)).ToList();
}
