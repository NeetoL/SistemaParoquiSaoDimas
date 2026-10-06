namespace SaoDimas.Dominio.Enums;

public enum StatusDistribuicao
{
    /// <summary>Tickets entregues à faixa, aguardando a prestação de contas.</summary>
    Distribuida = 1,

    /// <summary>Prestação de contas registrada: a faixa não pode mais ser alterada nem removida.</summary>
    PrestacaoRegistrada = 2
}
