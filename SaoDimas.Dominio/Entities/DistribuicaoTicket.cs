using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Faixa de tickets de um lote entregue (ou reservada) a um responsável opcional. Alterada somente pelo
/// agregado <see cref="LoteTicket"/>, que garante que as faixas não se sobreponham.
/// </summary>
public sealed class DistribuicaoTicket
{
    private DistribuicaoTicket(int loteTicketId, FaixaNumeracao faixa, Responsavel? responsavel, DateTime dataDistribuicao)
    {
        LoteTicketId = loteTicketId;
        NumeroInicial = faixa.Inicial;
        NumeroFinal = faixa.Final;
        Responsavel = responsavel;
        DataDistribuicao = dataDistribuicao;
        Status = StatusDistribuicao.Distribuida;
    }

    public int Id { get; private set; }

    public int LoteTicketId { get; private set; }

    public int NumeroInicial { get; private set; }

    public int NumeroFinal { get; private set; }

    /// <summary>Opcional: sem responsável, os tickets da faixa ainda não foram atribuídos a uma pessoa.</summary>
    public Responsavel? Responsavel { get; private set; }

    public DateTime DataDistribuicao { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    public StatusDistribuicao Status { get; private set; }

    public PrestacaoContas? Prestacao { get; private set; }

    public FaixaNumeracao Faixa => FaixaNumeracao.Reconstituir(NumeroInicial, NumeroFinal);

    public int Quantidade => NumeroFinal - NumeroInicial + 1;

    public bool PossuiResponsavel => Responsavel is not null;

    public bool PossuiPrestacao => Prestacao is not null;

    internal static DistribuicaoTicket Criar(int loteTicketId, FaixaNumeracao faixa, Responsavel? responsavel, DateTimeOffset agora) =>
        new(loteTicketId, faixa, responsavel, agora.UtcDateTime);

    internal void Alterar(FaixaNumeracao faixa, Responsavel? responsavel, DateTimeOffset agora)
    {
        NumeroInicial = faixa.Inicial;
        NumeroFinal = faixa.Final;
        Responsavel = responsavel;
        AtualizadoEm = agora.UtcDateTime;
    }

    internal void RegistrarPrestacao(PrestacaoContas prestacao, DateTimeOffset agora)
    {
        Prestacao = prestacao;
        Status = StatusDistribuicao.PrestacaoRegistrada;
        AtualizadoEm = agora.UtcDateTime;
    }
}
