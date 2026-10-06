namespace SaoDimas.Dominio.Entities;

public sealed partial class Evento
{
    // Mantém os identificadores operacionais existentes: cada Evento legado corresponde a uma edição.
    public EventoBase Base { get; private set; } = null!;
    public int Ano { get; private set; }
    public CaixaEvento Caixa { get; private set; } = new();

    public ModeloEvento ExportarModelo() => new(Base.Nome, Array.AsReadOnly(Produtos.Select(p =>
        new ProdutoModeloEvento(p.Nome, p.Descricao, p.Preco, p.Ativo)).ToArray()));

    public Resultado AplicarModelo(ModeloEvento modelo, bool precos, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(modelo);
        if (!PermiteAlteracoes || Produtos.Count != 0) return Resultado.Falha(new Erro(string.Empty, "O modelo só pode ser aplicado em uma edição vazia e editável."));
        var novos = new List<ProdutoEvento>();
        foreach (var produto in modelo.Produtos)
        {
            var criado = ProdutoEvento.Criar(Id, produto.Nome, produto.Descricao, precos ? produto.Preco : 0m, agora, precos && produto.Ativo);
            if (!criado.Sucesso) return Resultado.Falha(criado.Erros);
            if (novos.Any(p => string.Equals(p.Nome, criado.Valor.Nome, StringComparison.OrdinalIgnoreCase)))
                return Resultado.Falha(new Erro("Modelo", "O modelo contém produtos duplicados."));
            novos.Add(criado.Valor);
        }
        _produtos.AddRange(novos);
        return Resultado.Ok();
    }

    public Resultado ConfirmarFechamento(DateTimeOffset agora)
    {
        if (Caixa.Situacao != SituacaoCaixa.Fechado)
            return Resultado.Falha(new Erro(string.Empty, "Feche o caixa antes de finalizar a edição."));
        Status = SaoDimas.Dominio.Enums.StatusEvento.Fechado;
        AtualizadoEm = agora.UtcDateTime;
        return Resultado.Ok();
    }

    public Resultado<Evento> CriarEdicao(int ano, DateOnly inicio, DateOnly? fim, bool produtos, bool precos, DateTimeOffset agora)
    {
        if (ano < 1900 || ano > 9999 || inicio.Year != ano || fim < inicio)
            return Resultado<Evento>.Falha(new Erro("Ano", "Informe ano e período válidos para a edição."));
        var nova = new Evento(agora.UtcDateTime)
        {
            Base = Base, Ano = ano, Nome = Base.Nome, Descricao = Base.Descricao, ComunidadeId = Base.ComunidadeId,
            DataInicio = inicio, DataFim = fim
        };
        if (produtos)
        {
            var resultado = nova.AplicarModelo(ExportarModelo(), precos, agora);
            if (!resultado.Sucesso) return Resultado<Evento>.Falha(resultado.Erros);
        }
        return Resultado<Evento>.Ok(nova);
    }
}
