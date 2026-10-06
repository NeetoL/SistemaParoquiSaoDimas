using System.ComponentModel.DataAnnotations;

namespace SaoDimas.Aplicacao.Configuracoes;

/// <summary>
/// Dados institucionais da paróquia (seção "Paroquia" da configuração). Fonte única para documentos
/// impressos, como o Envelope de Dízimo. Campos opcionais vazios simplesmente não são impressos.
/// </summary>
public sealed class ConfiguracaoParoquia
{
    public const string Secao = "Paroquia";

    [Required(AllowEmptyStrings = false)]
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// Linha abaixo do nome no cabeçalho dos documentos (ex.: diocese). Opcional.
    /// </summary>
    public string? Subtitulo { get; init; }

    public string? Endereco { get; init; }

    public Dictionary<string, string> EnderecosComunidades { get; init; } = [];

    public string? Telefone { get; init; }

    public string? Email { get; init; }

    public string? Site { get; init; }

    /// <summary>
    /// Caminho (relativo à raiz do conteúdo da aplicação) da logo em resolução de impressão.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string CaminhoLogoImpressao { get; init; } = string.Empty;
}
