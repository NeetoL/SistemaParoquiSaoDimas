namespace SaoDimas.Aplicacao.Dtos;

/// <summary>
/// Dados do dizimista impressos no envelope; o CPF não faz parte do documento.
/// </summary>
public sealed record DizimistaIdentificacaoDto(int Id, string Codigo, string Nome, string Comunidade, string? Telefone = null, string? Endereco = null, string? Cep = null, string? Bairro = null, DateOnly? DataNascimento = null);

public sealed record ArquivoPdf(byte[] Conteudo, string NomeArquivo)
{
    public const string TipoConteudo = "application/pdf";
}
