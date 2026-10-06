using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Aplicacao.Dtos;

public sealed record EdicaoResumoDto(int Id, int Ano, DateOnly Inicio, DateOnly? Fim, StatusEvento Status, SituacaoCaixa Caixa);
public sealed record FaixaPrestacaoDto(int LoteId, int Id, string Produto, string Faixa, int Recebidos,
    int? Vendidos, int? Devolvidos, decimal Preco);
public sealed record ContaResponsavelDto(string Nome, IReadOnlyList<FaixaPrestacaoDto> Faixas, decimal Devido, decimal Prestado)
{
    public decimal Pendente => Devido - Prestado;
    public bool TicketsConferidos => Faixas.All(f => f.Vendidos is not null && f.Devolvidos is not null && f.Vendidos + f.Devolvidos == f.Recebidos);
    public string Situacao => Pendente == 0m && TicketsConferidos ? "Quitado" : Prestado > 0m ? "Parcial" : "Pendente";
}
public sealed record ResultadoProdutoDto(string Nome, int Gerados, int Vendidos, int Devolvidos, decimal Valor, int Cancelados = 0);
public sealed record DisponibilidadeTicketDto(int LoteId, string Produto, int Inicial, int Final);
public sealed record GestaoEventoDto(int Id, EventoBase Evento, int Ano, string Comunidade, DateOnly Inicio, DateOnly? Fim,
    IReadOnlyList<EdicaoResumoDto> Edicoes, EstadoCaixa Caixa, SituacaoCaixa SituacaoCaixa,
    IReadOnlyList<ContaResponsavelDto> Responsaveis, IReadOnlyList<ResultadoProdutoDto> Produtos,
    decimal Potencial, decimal Devido, decimal Prestado, decimal DinheiroTeorico, IReadOnlyList<string> Pendencias,
    bool PermiteOperacoes)
{
    public IReadOnlyList<DisponibilidadeTicketDto> Disponiveis { get; init; } = [];
    public decimal Pendente => Devido - Prestado;
    public decimal Total(FormaRecebimento forma) => Caixa.Movimentacoes.Where(m => m.Forma == forma).Sum(m => m.Valor);
}
