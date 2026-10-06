using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Lote de tickets numerados de um produto (ex.: Feijoada 001–100 a R$ 25,00). Raiz do agregado que distribui
/// o lote em faixas, cada uma com um responsável opcional (ex.: 001–020 João, 021–050 Maria, 051–075 Carlos,
/// 076–100 disponível). Os tickets não são armazenados um a um: são derivados do lote e das faixas.
/// </summary>
public sealed class LoteTicket
{
    public const int QuantidadeMaxima = 5000;

    private readonly List<DistribuicaoTicket> _distribuicoes = [];
    private readonly List<CancelamentoTickets> _cancelamentos = [];
    public IReadOnlyList<CancelamentoTickets> Cancelamentos => _cancelamentos.AsReadOnly();
    public int QuantidadeCancelada => _cancelamentos.Sum(c => c.Final - c.Inicial + 1);
    public bool TicketCancelado(int numero) => _cancelamentos.Any(c => numero >= c.Inicial && numero <= c.Final);

    private LoteTicket(int eventoId, int produtoEventoId, FaixaNumeracao faixa, decimal precoUnitario, DateTime criadoEm)
    {
        EventoId = eventoId;
        ProdutoEventoId = produtoEventoId;
        NumeroInicial = faixa.Inicial;
        NumeroFinal = faixa.Final;
        PrecoUnitario = precoUnitario;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public int EventoId { get; private set; }

    public int ProdutoEventoId { get; private set; }

    public int NumeroInicial { get; private set; }

    public int NumeroFinal { get; private set; }

    /// <summary>Preço do produto no momento da geração do lote.</summary>
    public decimal PrecoUnitario { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    public FaixaNumeracao Faixa => FaixaNumeracao.Reconstituir(NumeroInicial, NumeroFinal);

    public int Quantidade => NumeroFinal - NumeroInicial + 1;

    public decimal ValorPotencial => Quantidade * PrecoUnitario;

    /// <summary>Faixas em ordem de numeração.</summary>
    public IReadOnlyList<DistribuicaoTicket> Distribuicoes => _distribuicoes.OrderBy(d => d.NumeroInicial).ToList();

    /// <summary>Tickets atribuídos a uma pessoa.</summary>
    public int QuantidadeDistribuida => _distribuicoes.Where(d => d.PossuiResponsavel).Sum(d => d.Quantidade);

    /// <summary>Tickets ainda não atribuídos a uma pessoa (em faixas sem responsável ou fora de qualquer faixa).</summary>
    public int QuantidadeDisponivel => Quantidade - QuantidadeDistribuida - QuantidadeCancelada;

    public int QuantidadeEmFaixasSemResponsavel => _distribuicoes.Where(d => !d.PossuiResponsavel).Sum(d => d.Quantidade);

    /// <summary>Tickets fora de qualquer faixa.</summary>
    public int QuantidadeLivre => Quantidade - _distribuicoes.Sum(d => d.Quantidade) - QuantidadeCancelada;

    public int TotalResponsaveis => Responsaveis().Count;

    public bool PossuiPrestacoes => _distribuicoes.Any(d => d.PossuiPrestacao);

    public static Resultado<LoteTicket> Gerar(Evento evento, int produtoEventoId, int quantidade, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var produto = evento.ObterProduto(produtoEventoId);
        if (produto is null)
        {
            return Resultado<LoteTicket>.Falha(Erro.NaoEncontrado("Produto não encontrado."));
        }

        var precoVigente = produto.Preco;
        var faixa = evento.ReservarNumeracao(produtoEventoId, quantidade, agora);

        return faixa.Sucesso
            ? Resultado<LoteTicket>.Ok(new LoteTicket(evento.Id, produtoEventoId, faixa.Valor, precoVigente, agora.UtcDateTime))
            : Resultado<LoteTicket>.Falha(faixa.Erros);
    }

    public DistribuicaoTicket? ObterDistribuicao(int distribuicaoId) => _distribuicoes.FirstOrDefault(d => d.Id == distribuicaoId);

    /// <summary>Faixa que contém o número do ticket (nula se o número ainda não foi distribuído).</summary>
    public DistribuicaoTicket? DistribuicaoDoTicket(int numero) => _distribuicoes.FirstOrDefault(d => d.Faixa.Contem(numero));

    /// <summary>Responsável pelo ticket, descoberto pela faixa que contém o número.</summary>
    public Responsavel? ResponsavelDoTicket(int numero) => DistribuicaoDoTicket(numero)?.Responsavel;

    /// <summary>
    /// Trechos do lote fora de qualquer faixa (pode haver várias lacunas: 021–030, 041–050...).
    /// </summary>
    public IReadOnlyList<FaixaNumeracao> Lacunas()
    {
        var lacunas = new List<FaixaNumeracao>();
        var proximo = NumeroInicial;

        var ocupadas = Distribuicoes.Select(d => (NumeroInicial: d.NumeroInicial, NumeroFinal: d.NumeroFinal))
            .Concat(_cancelamentos.Select(c => (NumeroInicial: c.Inicial, NumeroFinal: c.Final))).OrderBy(f => f.NumeroInicial);
        foreach (var distribuicao in ocupadas)
        {
            if (distribuicao.NumeroInicial > proximo)
            {
                lacunas.Add(FaixaNumeracao.Reconstituir(proximo, distribuicao.NumeroInicial - 1));
            }

            proximo = Math.Max(proximo, distribuicao.NumeroFinal + 1);
        }

        if (proximo <= NumeroFinal)
        {
            lacunas.Add(FaixaNumeracao.Reconstituir(proximo, NumeroFinal));
        }

        return lacunas;
    }

    public Resultado CancelarTickets(Evento evento, int inicial, int final, string motivo, string operador, DateTimeOffset agora)
    {
        var erro = ValidarEvento(evento, exigirAlteracoes: false);
        if (erro is not null) return Resultado.Falha(erro);
        var faixa = FaixaNumeracao.Criar(inicial, final);
        if (!faixa.Sucesso) return Resultado.Falha(faixa.Erros);
        if (!Lacunas().Any(l => l.Contem(faixa.Valor)))
            return Resultado.Falha(new Erro(string.Empty, "Cancele somente tickets livres. Faixas distribuídas precisam ser resolvidas antes."));
        if (string.IsNullOrWhiteSpace(operador) || (motivo?.Trim().Length ?? 0) < 5)
            return Resultado.Falha(new Erro(string.Empty, "Informe o operador e o motivo do cancelamento."));
        _cancelamentos.Add(new(inicial, final, motivo!.Trim(), operador.Trim(), agora));
        evento.Caixa.Auditar(operador.Trim(), "Cancelamento de tickets", $"Lote {Id}, faixa {faixa.Valor} disponível",
            $"Cancelada; motivo: {motivo}", agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    /// <summary>Responsáveis distintos do lote (uma pessoa pode ter várias faixas).</summary>
    public IReadOnlyList<Responsavel> Responsaveis()
    {
        var responsaveis = new List<Responsavel>();
        foreach (var responsavel in Distribuicoes.Select(d => d.Responsavel).OfType<Responsavel>())
        {
            if (!responsaveis.Any(r => r.MesmaPessoa(responsavel)))
            {
                responsaveis.Add(responsavel);
            }
        }

        return responsaveis;
    }

    public Resultado<DistribuicaoTicket> Distribuir(Evento evento, int numeroInicial, int numeroFinal, string? responsavel, DateTimeOffset agora, string operador = "Sistema")
    {
        var validacao = ValidarFaixa(evento, numeroInicial, numeroFinal, responsavel, ignorarDistribuicaoId: null);
        if (!validacao.Sucesso)
        {
            return Resultado<DistribuicaoTicket>.Falha(validacao.Erros);
        }

        var distribuicao = DistribuicaoTicket.Criar(Id, validacao.Valor.Faixa, validacao.Valor.Responsavel, agora);
        _distribuicoes.Add(distribuicao);
        evento.Caixa.Auditar(operador, "Distribuição de tickets", $"Lote {Id}, faixa {distribuicao.Faixa} livre",
            $"Entregue a {distribuicao.Responsavel?.Nome ?? "sem responsável"}; {distribuicao.Quantidade} tickets", agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado<DistribuicaoTicket>.Ok(distribuicao);
    }

    public Resultado AlterarDistribuicao(
        Evento evento, int distribuicaoId, int numeroInicial, int numeroFinal, string? responsavel, DateTimeOffset agora, string operador = "Sistema")
    {
        var distribuicao = ObterDistribuicao(distribuicaoId);
        if (distribuicao is null)
        {
            return Resultado.Falha(Erro.NaoEncontrado("Faixa não encontrada."));
        }

        if (distribuicao.PossuiPrestacao)
        {
            return Resultado.Falha(FaixaComPrestacao());
        }

        var validacao = ValidarFaixa(evento, numeroInicial, numeroFinal, responsavel, ignorarDistribuicaoId: distribuicaoId);
        if (!validacao.Sucesso)
        {
            return Resultado.Falha(validacao.Erros);
        }

        var anterior = $"Lote {Id}, faixa {distribuicao.Faixa}, responsável {distribuicao.Responsavel?.Nome}";
        distribuicao.Alterar(validacao.Valor.Faixa, validacao.Valor.Responsavel, agora);
        evento.Caixa.Auditar(operador, "Alteração de distribuição", anterior,
            $"Faixa {distribuicao.Faixa}, responsável {distribuicao.Responsavel?.Nome}", agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    /// <summary>
    /// Cancela a faixa, devolvendo os tickets ao lote. Não é permitido após a prestação de contas.
    /// </summary>
    public Resultado RemoverDistribuicao(Evento evento, int distribuicaoId, DateTimeOffset agora, string operador = "Sistema")
    {
        var erroEvento = ValidarEvento(evento, exigirAlteracoes: true);
        if (erroEvento is not null)
        {
            return Resultado.Falha(erroEvento);
        }

        var distribuicao = ObterDistribuicao(distribuicaoId);
        if (distribuicao is null)
        {
            return Resultado.Falha(Erro.NaoEncontrado("Faixa não encontrada."));
        }

        if (distribuicao.PossuiPrestacao)
        {
            return Resultado.Falha(FaixaComPrestacao());
        }

        _distribuicoes.Remove(distribuicao);
        evento.Caixa.Auditar(operador, "Cancelamento de distribuição", $"Lote {Id}, faixa {distribuicao.Faixa}, responsável {distribuicao.Responsavel?.Nome}",
            "Tickets disponíveis novamente; números preservados", agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    public Resultado RegistrarPrestacao(
        Evento evento, int distribuicaoId, int vendidos, int devolvidos, decimal valorEntregue, string? justificativa, DateTimeOffset agora)
    {
        var erroEvento = ValidarEvento(evento, exigirAlteracoes: false);
        if (erroEvento is not null)
        {
            return Resultado.Falha(erroEvento);
        }

        var distribuicao = ObterDistribuicao(distribuicaoId);
        if (distribuicao is null)
        {
            return Resultado.Falha(Erro.NaoEncontrado("Faixa não encontrada."));
        }

        if (!distribuicao.PossuiResponsavel)
        {
            return Resultado.Falha(new Erro(string.Empty, "Informe o responsável da faixa antes de registrar a prestação de contas."));
        }

        if (distribuicao.PossuiPrestacao)
            return Resultado.Falha(new Erro(string.Empty, "A prestação confirmada não pode ser sobrescrita. Use a central de prestação de contas para novos lançamentos."));

        var prestacao = PrestacaoContas.Criar(distribuicao.Quantidade, vendidos, devolvidos, PrecoUnitario, valorEntregue, justificativa, agora);
        if (!prestacao.Sucesso)
        {
            return Resultado.Falha(prestacao.Erros);
        }

        distribuicao.RegistrarPrestacao(prestacao.Valor, agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    public Resultado RegistrarSituacao(Evento evento, int distribuicaoId, int vendidos, int devolvidos, string operador, DateTimeOffset agora)
    {
        var erro = ValidarEvento(evento, exigirAlteracoes: false);
        if (erro is not null) return Resultado.Falha(erro);
        var faixa = ObterDistribuicao(distribuicaoId);
        if (faixa is null || !faixa.PossuiResponsavel) return Resultado.Falha(new Erro(string.Empty, "Faixa com responsável não encontrada."));
        if (string.IsNullOrWhiteSpace(operador)) return Resultado.Falha(new Erro(string.Empty, "Informe o operador."));
        var anterior = faixa.Prestacao;
        if (vendidos < (anterior?.Vendidos ?? 0) || devolvidos < (anterior?.Devolvidos ?? 0))
            return Resultado.Falha(new Erro(string.Empty, "Quantidades já confirmadas não podem ser reduzidas silenciosamente."));
        var prestacao = PrestacaoContas.Criar(faixa.Quantidade, vendidos, devolvidos, PrecoUnitario, anterior?.ValorEntregue ?? 0m,
            "Situação registrada; recebimentos no livro do caixa.", agora, situacaoParcial: true);
        if (!prestacao.Sucesso) return Resultado.Falha(prestacao.Erros);
        faixa.RegistrarPrestacao(prestacao.Valor, agora);
        evento.Caixa.Auditar(operador.Trim(), "Venda / devolução", $"Faixa {Id}/{faixa.Id}: {faixa.Quantidade} recebidos; {anterior?.Vendidos ?? 0} vendidos; {anterior?.Devolvidos ?? 0} devolvidos",
            $"{vendidos} vendidos; {devolvidos} devolvidos; devido {vendidos * PrecoUnitario}", agora);
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    private Resultado<(FaixaNumeracao Faixa, Responsavel? Responsavel)> ValidarFaixa(
        Evento evento, int numeroInicial, int numeroFinal, string? responsavel, int? ignorarDistribuicaoId)
    {
        var erroEvento = ValidarEvento(evento, exigirAlteracoes: true);
        if (erroEvento is not null)
        {
            return Resultado<(FaixaNumeracao, Responsavel?)>.Falha(erroEvento);
        }

        var faixa = FaixaNumeracao.Criar(numeroInicial, numeroFinal);
        var pessoa = Responsavel.CriarOpcional(responsavel);
        if (!faixa.Sucesso || !pessoa.Sucesso)
        {
            return Resultado<(FaixaNumeracao, Responsavel?)>.Falha([.. faixa.Erros, .. pessoa.Erros]);
        }

        if (!Faixa.Contem(faixa.Valor))
        {
            return Resultado<(FaixaNumeracao, Responsavel?)>.Falha(new Erro(
                "NumeroInicial", $"A faixa {faixa.Valor} está fora do lote ({Faixa})."));
        }

        if (_cancelamentos.Any(c => faixa.Valor.Inicial <= c.Final && c.Inicial <= faixa.Valor.Final))
            return Resultado<(FaixaNumeracao, Responsavel?)>.Falha(new Erro("NumeroInicial", "A faixa inclui tickets cancelados."));

        var conflito = _distribuicoes.FirstOrDefault(d => d.Id != ignorarDistribuicaoId && d.Faixa.SobrepoeA(faixa.Valor));
        if (conflito is not null)
        {
            var dono = conflito.Responsavel is { } r ? $" ({r.Nome})" : " (sem responsável)";
            return Resultado<(FaixaNumeracao, Responsavel?)>.Falha(new Erro(
                "NumeroInicial", $"A faixa {faixa.Valor} se sobrepõe à faixa {conflito.Faixa}{dono}."));
        }

        return Resultado<(FaixaNumeracao, Responsavel?)>.Ok((faixa.Valor, pessoa.Valor));
    }

    private Erro? ValidarEvento(Evento evento, bool exigirAlteracoes)
    {
        ArgumentNullException.ThrowIfNull(evento);

        if (evento.Id != EventoId)
        {
            throw new InvalidOperationException("O evento informado não é o evento do lote.");
        }

        return exigirAlteracoes
            ? evento.PermiteAlteracoes ? null : new Erro(string.Empty, "O evento não permite mais alterar a distribuição de tickets.")
            : evento.PermitePrestacaoDeContas ? null : new Erro(string.Empty, "O evento foi cancelado.");
    }

    private static Erro FaixaComPrestacao() =>
        new(string.Empty, "Esta faixa já possui prestação de contas e não pode ser alterada nem removida.");
}

public sealed record CancelamentoTickets(int Inicial, int Final, string Motivo, string Operador, DateTimeOffset Data);
