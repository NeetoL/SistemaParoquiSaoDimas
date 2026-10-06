using SaoDimas.Dominio.Enums;

namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Comunidade da Paróquia São Dimas: a Matriz ou uma de suas Capelas.
/// Os registros (nomes, ordem, situação) vivem no banco e são administráveis.
/// </summary>
public sealed class Comunidade
{
    public const int NomeTamanhoMaximo = 100;

    private Comunidade(string nome, TipoComunidade tipo, int ordemExibicao, DateTime criadoEm)
    {
        Nome = nome;
        Tipo = tipo;
        OrdemExibicao = ordemExibicao;
        Ativa = true;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    /// <summary>
    /// Nome próprio da comunidade (ex.: "Santa Teresinha"). Ver <see cref="NomeCompleto"/>.
    /// </summary>
    public string Nome { get; private set; }

    public TipoComunidade Tipo { get; private set; }

    public bool Ativa { get; private set; }

    public int OrdemExibicao { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    /// <summary>
    /// Nome para exibição: "Paróquia São Dimas", "Capela Santa Teresinha".
    /// </summary>
    public string NomeCompleto => FormatarNome(Tipo, Nome);

    /// <summary>
    /// Somente comunidades ativas recebem novos vínculos (ex.: novos dizimistas).
    /// </summary>
    public bool PodeReceberVinculos => Ativa;

    public static string FormatarNome(TipoComunidade tipo, string nome) =>
        tipo == TipoComunidade.Capela ? $"Capela {nome}" : nome;

    public static Resultado<Comunidade> Criar(string nome, TipoComunidade tipo, int ordemExibicao, DateTimeOffset agora)
    {
        var nomeNormalizado = Texto.NormalizarEspacos(nome);
        var erros = new List<Erro>();

        if (nomeNormalizado.Length == 0)
        {
            erros.Add(new Erro(nameof(Nome), "Informe o nome da comunidade."));
        }
        else if (nomeNormalizado.Length > NomeTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Nome), $"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres."));
        }

        if (!Enum.IsDefined(tipo))
        {
            erros.Add(new Erro(nameof(Tipo), "Tipo de comunidade inválido."));
        }

        if (ordemExibicao < 0)
        {
            erros.Add(new Erro(nameof(OrdemExibicao), "A ordem de exibição não pode ser negativa."));
        }

        return erros.Count > 0
            ? Resultado<Comunidade>.Falha(erros)
            : Resultado<Comunidade>.Ok(new Comunidade(nomeNormalizado, tipo, ordemExibicao, agora.UtcDateTime));
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativa)
        {
            return;
        }

        Ativa = true;
        AtualizadoEm = agora.UtcDateTime;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativa)
        {
            return;
        }

        Ativa = false;
        AtualizadoEm = agora.UtcDateTime;
    }
}
