namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Produto vendido em um evento (feijoada, refrigerante, água...). Alterado somente pelo agregado <see cref="Evento"/>.
/// </summary>
public sealed class ProdutoEvento
{
    public const int NomeTamanhoMaximo = 100;
    public const int DescricaoTamanhoMaximo = 500;
    public const decimal PrecoMaximo = 100_000m;

    private ProdutoEvento(int eventoId, DateTime criadoEm)
    {
        EventoId = eventoId;
        Nome = string.Empty;
        Ativo = true;
        ProximoNumeroTicket = 1;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public int EventoId { get; private set; }

    public string Nome { get; private set; }

    public string? Descricao { get; private set; }

    /// <summary>
    /// Preço atual. Cada lote guarda o preço vigente na sua geração; mudar o preço não altera lotes existentes.
    /// </summary>
    public decimal Preco { get; private set; }

    public bool Ativo { get; private set; }

    /// <summary>
    /// Próximo número de ticket disponível para este produto (a numeração é sequencial e não se repete).
    /// </summary>
    public int ProximoNumeroTicket { get; private set; }

    public int TicketsGerados => ProximoNumeroTicket - 1;

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    internal static Resultado<ProdutoEvento> Criar(int eventoId, string nome, string? descricao, decimal preco, DateTimeOffset agora, bool ativo = true)
    {
        var produto = new ProdutoEvento(eventoId, agora.UtcDateTime);
        var resultado = produto.Aplicar(nome, descricao, preco, ativo);

        return resultado.Sucesso ? Resultado<ProdutoEvento>.Ok(produto) : Resultado<ProdutoEvento>.Falha(resultado.Erros);
    }

    internal Resultado Atualizar(string nome, string? descricao, decimal preco, bool ativo, DateTimeOffset agora)
    {
        var resultado = Aplicar(nome, descricao, preco, ativo);
        if (resultado.Sucesso)
        {
            AtualizadoEm = agora.UtcDateTime;
        }

        return resultado;
    }

    internal void AvancarNumeracao(int proximoNumero, DateTimeOffset agora)
    {
        ProximoNumeroTicket = proximoNumero;
        AtualizadoEm = agora.UtcDateTime;
    }

    private Resultado Aplicar(string nome, string? descricao, decimal preco, bool ativo)
    {
        var erros = new List<Erro>();

        var nomeNormalizado = Texto.NormalizarEspacos(nome);
        if (nomeNormalizado.Length == 0)
        {
            erros.Add(new Erro(nameof(Nome), "Informe o nome do produto."));
        }
        else if (nomeNormalizado.Length > NomeTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Nome), $"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres."));
        }

        var descricaoNormalizada = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        if (descricaoNormalizada?.Length > DescricaoTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Descricao), $"A descrição deve ter no máximo {DescricaoTamanhoMaximo} caracteres."));
        }

        if (preco < 0 || preco == 0 && ativo || preco > PrecoMaximo || !Monetario.EhValido(preco))
        {
            erros.Add(new Erro(nameof(Preco), "Informe um preço maior que zero, com até duas casas decimais."));
        }

        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Nome = nomeNormalizado;
        Descricao = descricaoNormalizada;
        Preco = preco;
        Ativo = ativo;
        return Resultado.Ok();
    }
}
