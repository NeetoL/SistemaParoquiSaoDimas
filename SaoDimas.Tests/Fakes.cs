using System.Reflection;
using SaoDimas.Dominio.Enums;
using ComunidadeEntidade = SaoDimas.Dominio.Entities.Comunidade;
using DizimistaEntidade = SaoDimas.Dominio.Entities.Dizimista;

namespace SaoDimas.Tests;

/// <summary>
/// Relógio fixo para testes (horário local = UTC).
/// </summary>
internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public static readonly DateTimeOffset Padrao = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    public RelogioFixo() : this(Padrao) { }

    public override DateTimeOffset GetUtcNow() => agora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal static class Criar
{
    public static readonly DateOnly Hoje = DateOnly.FromDateTime(RelogioFixo.Padrao.DateTime);

    /// <summary>
    /// Comunidade como se tivesse sido lida do banco (com Id).
    /// </summary>
    public static ComunidadeEntidade Comunidade(int id, TipoComunidade tipo = TipoComunidade.Capela, bool ativa = true, string? nome = null)
    {
        var comunidade = ComunidadeEntidade.Criar(nome ?? $"Comunidade {id}", tipo, id, RelogioFixo.Padrao).Valor;
        DefinirId(comunidade, id);

        if (!ativa)
        {
            comunidade.Inativar(RelogioFixo.Padrao);
        }

        return comunidade;
    }

    public static DizimistaEntidade Dizimista(int id, ComunidadeEntidade comunidade, string? cpf = null)
    {
        var dizimista = DizimistaEntidade.Criar(
            "Maria das Graças", cpf, null, comunidade, Hoje, StatusDizimista.Ativo, RelogioFixo.Padrao).Valor;
        DefinirId(dizimista, id);

        return dizimista;
    }

    /// <summary>Simula a persistência atribuindo o Id (como o banco faria).</summary>
    public static T ComId<T>(T entidade, int id)
    {
        DefinirId(entidade, id);
        return entidade;
    }

    private static void DefinirId<T>(T entidade, int id) =>
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entidade, id);
}

internal sealed class AmbienteDeTeste : Microsoft.Extensions.Hosting.IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testes";

    public string ApplicationName { get; set; } = "SaoDimas.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}
