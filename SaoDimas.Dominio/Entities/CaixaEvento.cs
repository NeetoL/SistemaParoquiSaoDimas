using SaoDimas.Dominio.Enums;

namespace SaoDimas.Dominio.Entities;

public enum SituacaoCaixa { NaoAberto, Aberto, EmConferencia, Fechado }
public enum FormaRecebimento { Dinheiro = 1, Pix, Transferencia, Outro }
public sealed record EventoBase(Guid Id, string Nome, string? Descricao, int ComunidadeId, bool Ativo);
public sealed record AberturaCaixa(DateTimeOffset Data, string Operador, decimal FundoTroco, string? Observacao);
public sealed record MovimentacaoCaixa(Guid Id, string Responsavel, decimal Valor, FormaRecebimento Forma,
    string Operador, DateTimeOffset Data, string? Observacao, Guid? OriginalId = null);
public sealed record ConferenciaCaixa(decimal Contado, decimal Teorico, string? Justificativa, string Operador,
    DateTimeOffset Data, int Revisao);
public sealed record FechamentoCaixa(string Operador, DateTimeOffset Data, string? Motivo,
    string? Justificativa, IReadOnlyList<string> Pendencias, IdentidadeFechamento? Identidade = null);
public sealed record IdentidadeFechamento(string Evento, int Ano, string Comunidade, DateOnly Inicio, DateOnly? Fim);
public sealed record AuditoriaEvento(Guid Id, DateTimeOffset Data, string Operador, string Acao, string Antes, string Depois);
public sealed record EstadoCaixa(AberturaCaixa? Abertura, ConferenciaCaixa? Conferencia, FechamentoCaixa? Fechamento,
    IReadOnlyList<MovimentacaoCaixa> Movimentacoes, IReadOnlyList<AuditoriaEvento> Historico, int Revisao);

/// <summary>Livro financeiro da edição: lançamentos imutáveis e estornos por contrapartida.</summary>
public sealed class CaixaEvento
{
    private readonly List<MovimentacaoCaixa> _movimentacoes = [];
    private readonly List<AuditoriaEvento> _historico = [];
    public AberturaCaixa? Abertura { get; private set; }
    public ConferenciaCaixa? Conferencia { get; private set; }
    public FechamentoCaixa? Fechamento { get; private set; }
    public int Revisao { get; private set; }
    public SituacaoCaixa Situacao => Fechamento is not null ? SituacaoCaixa.Fechado : Abertura is null
        ? SituacaoCaixa.NaoAberto : Conferencia?.Revisao == Revisao ? SituacaoCaixa.EmConferencia : SituacaoCaixa.Aberto;
    public IReadOnlyList<MovimentacaoCaixa> Movimentacoes => _movimentacoes.AsReadOnly();
    public IReadOnlyList<AuditoriaEvento> Historico => _historico.AsReadOnly();
    public decimal Receita => _movimentacoes.Sum(m => m.Valor);
    public decimal Total(FormaRecebimento forma) => _movimentacoes.Where(m => m.Forma == forma).Sum(m => m.Valor);
    public decimal Prestado(string responsavel) => _movimentacoes.Where(m => string.Equals(m.Responsavel, responsavel,
        StringComparison.OrdinalIgnoreCase)).Sum(m => m.Valor);
    public decimal DinheiroTeorico => (Abertura?.FundoTroco ?? 0m) + Total(FormaRecebimento.Dinheiro);

    public void ImportarPrestacaoLegada(string responsavel, decimal valor, DateTime data, string referencia)
    {
        if (Abertura is null || Fechamento is not null) throw new InvalidOperationException("Importação somente na abertura.");
        _movimentacoes.Add(new(Guid.NewGuid(), responsavel, valor, FormaRecebimento.Outro,
            "Importação do histórico anterior (operador não informado)", new DateTimeOffset(DateTime.SpecifyKind(data, DateTimeKind.Utc)),
            referencia + "; forma de recebimento não informada no cadastro anterior"));
        Auditar("Sistema", "Importação", referencia, $"Recebimento histórico: {valor}", Abertura.Data);
    }

    public Resultado Abrir(decimal troco, string operador, string? observacao, DateTimeOffset agora)
    {
        if (Abertura is not null) return Falha("O caixa já foi aberto.");
        if (string.IsNullOrWhiteSpace(operador)) return Falha("Informe quem abriu o caixa.");
        if (troco < 0m || !Monetario.EhValido(troco)) return Falha("Informe um fundo de troco válido.");
        Abertura = new(agora, operador.Trim(), troco, observacao?.Trim());
        Auditar(operador, "Abertura", "Não aberto", $"Troco: {troco}", agora);
        return Resultado.Ok();
    }

    public Resultado Receber(string responsavel, decimal valor, decimal devido, FormaRecebimento forma,
        string operador, string? observacao, DateTimeOffset agora, Guid? requisicaoId = null)
    {
        if (Abertura is null || Fechamento is not null) return Falha("Abra o caixa antes de receber. Caixa fechado não permite lançamentos.");
        if (string.IsNullOrWhiteSpace(operador) || string.IsNullOrWhiteSpace(responsavel)) return Falha("Informe responsável e operador.");
        if (!Enum.IsDefined(forma) || valor <= 0 || !Monetario.EhValido(valor)) return Falha("Informe forma e valor válidos.");
        if (requisicaoId is { } chave && _movimentacoes.FirstOrDefault(m => m.Id == chave) is { } existente)
            return existente.OriginalId is null && existente.Valor == valor && existente.Forma == forma &&
                string.Equals(existente.Responsavel, responsavel, StringComparison.OrdinalIgnoreCase)
                ? Resultado.Ok() : Falha("A confirmação já foi utilizada para outro lançamento.");
        var anterior = Prestado(responsavel);
        if (valor > devido - anterior) return Falha("O valor entregue ultrapassa o saldo pendente.");
        _movimentacoes.Add(new(requisicaoId ?? Guid.NewGuid(), responsavel, valor, forma, operador.Trim(), agora, observacao?.Trim()));
        Auditar(operador, "Prestação", $"Prestado: {anterior}", $"Prestado: {anterior + valor}", agora);
        return Resultado.Ok();
    }

    public Resultado Estornar(Guid id, string motivo, string operador, DateTimeOffset agora)
    {
        if (Abertura is null || Fechamento is not null) return Falha("O caixa não permite estorno.");
        var original = _movimentacoes.FirstOrDefault(m => m.Id == id && m.OriginalId is null);
        if (original is null || _movimentacoes.Any(m => m.OriginalId == id)) return Falha("Lançamento inexistente ou já estornado.");
        if (string.IsNullOrWhiteSpace(operador) || (motivo?.Trim().Length ?? 0) < 5) return Falha("Informe operador e motivo do estorno (mínimo 5 caracteres).");
        _movimentacoes.Add(new(Guid.NewGuid(), original.Responsavel, -original.Valor, original.Forma,
            operador.Trim(), agora, motivo!.Trim(), original.Id));
        Auditar(operador, "Estorno", $"Lançamento: {original.Id}, valor: {original.Valor}", $"Contrapartida: {-original.Valor}", agora);
        return Resultado.Ok();
    }

    public Resultado Conferir(decimal contado, string? justificativa, string operador, DateTimeOffset agora)
    {
        if (Abertura is null || Fechamento is not null) return Falha("O caixa não permite conferência.");
        if (string.IsNullOrWhiteSpace(operador) || contado < 0m || !Monetario.EhValido(contado)) return Falha("Informe operador e valor contado válidos.");
        if (contado != DinheiroTeorico && (justificativa?.Trim().Length ?? 0) < 5) return Falha("Justifique a diferença de caixa.");
        Auditar(operador, "Conferência", $"Teórico: {DinheiroTeorico}", $"Contado: {contado}; {justificativa}", agora);
        Conferencia = new(contado, DinheiroTeorico, justificativa?.Trim(), operador.Trim(), agora, Revisao);
        return Resultado.Ok();
    }

    public Resultado Fechar(IReadOnlyList<string> pendencias, bool excepcional, string? motivo,
        string? justificativa, string operador, DateTimeOffset agora, IdentidadeFechamento? identidade = null)
    {
        if (Situacao != SituacaoCaixa.EmConferencia) return Falha("Realize uma conferência atualizada antes do fechamento.");
        if (string.IsNullOrWhiteSpace(operador)) return Falha("Informe o responsável pelo fechamento.");
        if (pendencias.Count > 0 && !excepcional) return Falha("Existem pendências: " + string.Join(" ", pendencias));
        if (pendencias.Count > 0 && ((motivo?.Trim().Length ?? 0) < 5 || (justificativa?.Trim().Length ?? 0) < 5))
            return Falha("O fechamento excepcional exige motivo e justificativa.");
        Fechamento = new(operador.Trim(), agora, motivo?.Trim(), justificativa?.Trim(), Array.AsReadOnly(pendencias.ToArray()), identidade);
        Auditar(operador, pendencias.Count > 0 ? "Fechamento com pendências" : "Fechamento", "Em conferência", "Fechado", agora);
        return Resultado.Ok();
    }

    public void Auditar(string operador, string acao, string antes, string depois, DateTimeOffset agora)
    {
        _historico.Add(new(Guid.NewGuid(), agora, operador, acao, antes, depois));
        Revisao++;
    }
    public EstadoCaixa Exportar() => new(Abertura, Conferencia, Fechamento, Movimentacoes, Historico, Revisao);
    public static CaixaEvento Reconstituir(EstadoCaixa? estado)
    {
        var caixa = new CaixaEvento();
        if (estado is null) return caixa;
        caixa.Abertura = estado.Abertura; caixa.Conferencia = estado.Conferencia; caixa.Fechamento = estado.Fechamento;
        caixa._movimentacoes.AddRange(estado.Movimentacoes); caixa._historico.AddRange(estado.Historico); caixa.Revisao = estado.Revisao;
        return caixa;
    }
    private static Resultado Falha(string mensagem) => Resultado.Falha(new Erro(string.Empty, mensagem));
}
