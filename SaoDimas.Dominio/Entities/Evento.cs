using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Evento da paróquia ou de uma capela (festa, almoço, feijoada...). Raiz do agregado que contém os produtos
/// vendidos e controla a numeração dos tickets de cada produto.
/// </summary>
public sealed partial class Evento
{
    public const int NomeTamanhoMaximo = 150;
    public const int DescricaoTamanhoMaximo = 1000;
    public const int ObservacoesTamanhoMaximo = 2000;

    private readonly List<ProdutoEvento> _produtos = [];

    private Evento(DateTime criadoEm)
    {
        Nome = string.Empty;
        Status = StatusEvento.Planejamento;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string? Descricao { get; private set; }

    public int ComunidadeId { get; private set; }

    public DateOnly DataInicio { get; private set; }

    public DateOnly? DataFim { get; private set; }

    public StatusEvento Status { get; private set; }

    public string? Observacoes { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    public IReadOnlyList<ProdutoEvento> Produtos => _produtos.AsReadOnly();

    /// <summary>
    /// Cadastro, produtos, lotes e distribuições só podem ser alterados em eventos em planejamento ou ativos.
    /// </summary>
    public bool PermiteAlteracoes => Caixa.Situacao != SituacaoCaixa.Fechado && Status is StatusEvento.Planejamento or StatusEvento.Ativo;

    /// <summary>
    /// A prestação de contas continua possível depois do encerramento (ela normalmente acontece após o evento).
    /// </summary>
    public bool PermitePrestacaoDeContas => Caixa.Situacao != SituacaoCaixa.Fechado && Status is not StatusEvento.Cancelado;

    public static Resultado<Evento> Criar(
        string nome, string? descricao, Comunidade comunidade, DateOnly dataInicio, DateOnly? dataFim, string? observacoes,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(comunidade);

        var evento = new Evento(agora.UtcDateTime);
        var resultado = evento.Aplicar(nome, descricao, comunidade, novoVinculo: true, dataInicio, dataFim, observacoes);
        evento.Base = new EventoBase(Guid.NewGuid(), evento.Nome, evento.Descricao, evento.ComunidadeId, true);
        evento.Ano = dataInicio.Year;

        return resultado.Sucesso ? Resultado<Evento>.Ok(evento) : Resultado<Evento>.Falha(resultado.Erros);
    }

    public Resultado Atualizar(
        string nome, string? descricao, Comunidade comunidade, DateOnly dataInicio, DateOnly? dataFim, string? observacoes,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(comunidade);

        if (!PermiteAlteracoes)
        {
            return Resultado.Falha(EventoBloqueado());
        }

        var resultado = Aplicar(nome, descricao, comunidade, novoVinculo: comunidade.Id != ComunidadeId, dataInicio, dataFim, observacoes);
        if (resultado.Sucesso)
        {
            Base = Base with { Nome = Nome, Descricao = Descricao, ComunidadeId = ComunidadeId };
            AtualizadoEm = agora.UtcDateTime;
        }

        return resultado;
    }

    public Resultado Ativar(DateTimeOffset agora) =>
        MudarStatus(StatusEvento.Ativo, agora, permitidoDe: [StatusEvento.Planejamento]);

    public Resultado Encerrar(DateTimeOffset agora) =>
        MudarStatus(StatusEvento.Encerrado, agora, permitidoDe: [StatusEvento.Planejamento, StatusEvento.Ativo]);

    public Resultado Cancelar(DateTimeOffset agora) =>
        MudarStatus(StatusEvento.Cancelado, agora, permitidoDe: [StatusEvento.Planejamento, StatusEvento.Ativo]);

    public ProdutoEvento? ObterProduto(int produtoId) => _produtos.FirstOrDefault(produto => produto.Id == produtoId);

    public Resultado<ProdutoEvento> AdicionarProduto(string nome, string? descricao, decimal preco, DateTimeOffset agora)
    {
        if (!PermiteAlteracoes)
        {
            return Resultado<ProdutoEvento>.Falha(EventoBloqueado());
        }

        var resultado = ProdutoEvento.Criar(Id, nome, descricao, preco, agora);
        if (!resultado.Sucesso)
        {
            return resultado;
        }

        if (NomeEmUso(resultado.Valor.Nome, ignorarProdutoId: null))
        {
            return Resultado<ProdutoEvento>.Falha(NomeDuplicado());
        }

        _produtos.Add(resultado.Valor);
        AtualizadoEm = agora.UtcDateTime;
        return resultado;
    }

    public Resultado AtualizarProduto(int produtoId, string nome, string? descricao, decimal preco, bool ativo, DateTimeOffset agora)
    {
        if (!PermiteAlteracoes)
        {
            return Resultado.Falha(EventoBloqueado());
        }

        var produto = ObterProduto(produtoId);
        if (produto is null)
        {
            return Resultado.Falha(Erro.NaoEncontrado("Produto não encontrado."));
        }

        var nomeNormalizado = Texto.NormalizarEspacos(nome);
        if (nomeNormalizado.Length > 0 && NomeEmUso(nomeNormalizado, ignorarProdutoId: produtoId))
        {
            return Resultado.Falha(NomeDuplicado());
        }

        return produto.Atualizar(nome, descricao, preco, ativo, agora);
    }

    /// <summary>
    /// Reserva a próxima faixa de numeração do produto para um novo lote (ex.: 001–100, depois 101–150).
    /// Os números nunca se repetem dentro do produto. Na persistência relacional, a concorrência é protegida
    /// pelo controle otimista do agregado (o "próximo número" faz parte do Evento), sem depender de MAX()+1.
    /// </summary>
    public Resultado<FaixaNumeracao> ReservarNumeracao(int produtoId, int quantidade, DateTimeOffset agora)
    {
        if (!PermiteAlteracoes)
        {
            return Resultado<FaixaNumeracao>.Falha(EventoBloqueado());
        }

        var produto = ObterProduto(produtoId);
        if (produto is null)
        {
            return Resultado<FaixaNumeracao>.Falha(Erro.NaoEncontrado("Produto não encontrado."));
        }

        if (!produto.Ativo)
        {
            return Resultado<FaixaNumeracao>.Falha(new Erro(string.Empty, "Produto inativo não pode receber novos tickets."));
        }

        if (quantidade < 1 || quantidade > LoteTicket.QuantidadeMaxima)
        {
            return Resultado<FaixaNumeracao>.Falha(new Erro("Quantidade", $"Informe uma quantidade entre 1 e {LoteTicket.QuantidadeMaxima}."));
        }

        var faixa = FaixaNumeracao.Criar(produto.ProximoNumeroTicket, produto.ProximoNumeroTicket + quantidade - 1, "Quantidade", "Quantidade");
        if (!faixa.Sucesso)
        {
            return faixa;
        }

        produto.AvancarNumeracao(faixa.Valor.Final + 1, agora);
        AtualizadoEm = agora.UtcDateTime;
        return faixa;
    }

    private Resultado Aplicar(
        string nome, string? descricao, Comunidade comunidade, bool novoVinculo, DateOnly dataInicio, DateOnly? dataFim, string? observacoes)
    {
        var erros = new List<Erro>();

        var nomeNormalizado = Texto.NormalizarEspacos(nome);
        if (nomeNormalizado.Length == 0)
        {
            erros.Add(new Erro(nameof(Nome), "Informe o nome do evento."));
        }
        else if (nomeNormalizado.Length > NomeTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Nome), $"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres."));
        }

        var descricaoNormalizada = TextoOpcional(descricao);
        if (descricaoNormalizada?.Length > DescricaoTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Descricao), $"A descrição deve ter no máximo {DescricaoTamanhoMaximo} caracteres."));
        }

        var observacoesNormalizadas = TextoOpcional(observacoes);
        if (observacoesNormalizadas?.Length > ObservacoesTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Observacoes), $"As observações devem ter no máximo {ObservacoesTamanhoMaximo} caracteres."));
        }

        if (novoVinculo && !comunidade.PodeReceberVinculos)
        {
            erros.Add(new Erro(nameof(ComunidadeId), "A comunidade selecionada está inativa."));
        }

        if (dataFim is { } fim && fim < dataInicio)
        {
            erros.Add(new Erro(nameof(DataFim), "A data final não pode ser anterior à data inicial."));
        }

        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Nome = nomeNormalizado;
        Descricao = descricaoNormalizada;
        ComunidadeId = comunidade.Id;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Observacoes = observacoesNormalizadas;
        return Resultado.Ok();
    }

    private Resultado MudarStatus(StatusEvento novo, DateTimeOffset agora, StatusEvento[] permitidoDe)
    {
        if (Status == novo)
        {
            return Resultado.Ok();
        }

        if (!permitidoDe.Contains(Status))
        {
            return Resultado.Falha(new Erro(nameof(Status), $"Não é possível alterar um evento {Status.ToString().ToLowerInvariant()} para {novo.ToString().ToLowerInvariant()}."));
        }

        Status = novo;
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    private bool NomeEmUso(string nome, int? ignorarProdutoId) =>
        _produtos.Any(produto => produto.Id != ignorarProdutoId
                                 && string.Equals(produto.Nome, nome, StringComparison.OrdinalIgnoreCase));

    private static Erro NomeDuplicado() => new(nameof(ProdutoEvento.Nome), "Já existe um produto com este nome no evento.");

    private Erro EventoBloqueado() =>
        new(string.Empty, $"O evento está {Status.ToString().ToLowerInvariant()} e não pode ser alterado.");

    private static string? TextoOpcional(string? texto)
    {
        var normalizado = texto?.Trim();
        return string.IsNullOrEmpty(normalizado) ? null : normalizado;
    }
}
