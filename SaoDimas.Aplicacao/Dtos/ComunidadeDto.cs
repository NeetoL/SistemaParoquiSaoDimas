namespace SaoDimas.Aplicacao.Dtos;

/// <param name="Nome">Nome completo para exibição (ex.: "Capela Santa Teresinha").</param>
public sealed record ComunidadeDto(int Id, string Nome, bool Ativa);
