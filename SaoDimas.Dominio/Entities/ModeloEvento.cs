namespace SaoDimas.Dominio.Entities;

/// <summary>Modelo contém somente configurações reutilizáveis; nenhum registro operacional ou financeiro.</summary>
public sealed record ProdutoModeloEvento(string Nome, string? Descricao, decimal Preco, bool Ativo);
public sealed record ModeloEvento(string Nome, IReadOnlyList<ProdutoModeloEvento> Produtos);
