using SaoDimas.Dominio.ValueObjects;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Aplicacao.Dtos;

/// <summary>
/// Dados informados no cadastro e na edição de um dizimista.
/// </summary>
public sealed record DadosDizimista(
    string Nome,
    string? Cpf,
    string? Telefone,
    int ComunidadeId,
    DateOnly DataEntrada,
    StatusDizimista Status, string? Endereco = null, string? Cep = null, string? Bairro = null, Aniversario? DataNascimento = null, string? CodigoOriginal = null);

public sealed record DizimistaResumoDto(
    int Id,
    string Codigo,
    string Nome,
    string Comunidade,
    string? Telefone,
    StatusDizimista Status, Aniversario? DataNascimento = null, string? Bairro = null, DateOnly? DataEntrada = null);

public sealed record DizimistaDetalhesDto(
    int Id,
    string Codigo,
    string Nome,
    string? Cpf,
    string? Telefone,
    string Comunidade,
    DateOnly DataEntrada,
    StatusDizimista Status,
    DateTime CriadoEm,
    DateTime? AtualizadoEm, string? Endereco = null, string? Cep = null, string? Bairro = null, Aniversario? DataNascimento = null);
