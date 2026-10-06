using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Aplicacao.Utilitarios;
namespace SaoDimas.Tests.Infraestrutura;

public sealed partial class PersistenciaJsonTests
{
    private static async Task<Guid> CriarRifaTeste(Microsoft.Extensions.DependencyInjection.ServiceProvider provider)
    {
        var resultado = await Requisicao<IRifasAplicacao, ResultadoGestao>(provider, app => app.CriarAsync(new("Rifa teste", "Bicicleta\nCesta", DateOnly.FromDateTime(DateTime.Today.AddDays(30)), 10m, 200, 1), "admin", CancellationToken.None)); Assert.True(resultado.Sucesso); return resultado.Id!.Value;
    }
    [Fact]
    public async Task Rifa_reserva_pagamento_resultado_e_backup_sobrevivem_ao_reinicio()
    {
        using var provider = Provider(); var id = await CriarRifaTeste(provider);
        async Task<ResultadoGestao> Acao(Func<IRifasAplicacao, Task<ResultadoGestao>> a) => await Requisicao<IRifasAplicacao, ResultadoGestao>(provider, a);
        Assert.True((await Acao(a => a.ReservarAsync(id, 1, "1,2", "Ana teste", "11999999999", "Equipe", "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.ReservarAsync(id, 2, "2,3", "Outra pessoa", "", "", "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.PagamentoAsync(id, 1, 1, true, "PIX", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Acao(a => a.PagamentoAsync(id, 2, 1, true, "PIX", "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.LiberarAsync(id, 3, 1, "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.SituacaoAsync(id, 3, "Cancelada", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Acao(a => a.SituacaoAsync(id, 3, "Fechada", "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.ResultadoAsync(id, 4, 1, 2, "Ata teste", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Acao(a => a.ResultadoAsync(id, 4, 1, 1, "Ata teste", "admin", CancellationToken.None))).Sucesso);
        Assert.False((await Acao(a => a.PagamentoAsync(id, 5, 1, false, "", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Acao(a => a.ResultadoAsync(id, 5, 2, 1, "Ata teste", "admin", CancellationToken.None))).Sucesso);
        using var reiniciado = Provider(); var r = await Requisicao<IRifasAplicacao, RifaDto?>(reiniciado, a => a.ObterAsync(id, CancellationToken.None));
        Assert.Equal("Concluída", r!.Situacao); Assert.Equal(2, r.Numeros.Count); Assert.Equal(2, r.Resultados.Count); Assert.True(ValidacaoRifas.Valida(r));
        var bytes = await Requisicao<IGestaoParoquialDados, byte[]>(reiniciado, a => a.CriarBackupAsync(CancellationToken.None)); Assert.Contains("rifas", System.Text.Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        var nome = await Requisicao<IGestaoParoquialDados, string>(reiniciado, a => a.ReceberBackupAsync(bytes, CancellationToken.None)); Assert.NotEmpty(nome);
    }
    [Fact]
    public async Task Rifa_concorrente_reserva_um_numero_uma_unica_vez()
    {
        using var p1 = Provider(); using var p2 = Provider(); var id = await CriarRifaTeste(p1);
        var respostas = await Task.WhenAll(new[] { p1, p2 }.Select(p => Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.ReservarAsync(id, 1, "7", "Pessoa teste", "", "", "admin", CancellationToken.None))));
        Assert.Single(respostas, r => r.Sucesso);
        var rifa = await Requisicao<IRifasAplicacao, RifaDto?>(p1, a => a.ObterAsync(id, CancellationToken.None)); Assert.Equal(7, Assert.Single(rifa!.Numeros).Numero);
    }
    [Theory]
    [InlineData("1,1")]
    [InlineData("0")]
    [InlineData("201")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task Rifa_recusa_numeros_invalidos_sem_reserva_parcial(string numeros) { using var provider = Provider(); var id = await CriarRifaTeste(provider); Assert.False((await Requisicao<IRifasAplicacao, ResultadoGestao>(provider, a => a.ReservarAsync(id, 1, numeros, "Pessoa", "", "", "admin", CancellationToken.None))).Sucesso); var r = await Requisicao<IRifasAplicacao, RifaDto?>(provider, a => a.ObterAsync(id, CancellationToken.None)); Assert.Empty(r!.Numeros); }
    [Fact]
    public async Task Rifa_liberacao_estorno_e_cancelamento_preservam_auditoria()
    {
        using var p = Provider(); var id = await CriarRifaTeste(p);
        Assert.True((await Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.ReservarAsync(id, 1, "5", "Pessoa", "", "", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.PagamentoAsync(id, 2, 5, true, "Dinheiro", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.PagamentoAsync(id, 3, 5, false, "", "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.LiberarAsync(id, 4, 5, "admin", CancellationToken.None))).Sucesso);
        Assert.True((await Requisicao<IRifasAplicacao, ResultadoGestao>(p, a => a.SituacaoAsync(id, 5, "Cancelada", "admin", CancellationToken.None))).Sucesso);
        var aud = await Requisicao<IGestaoParoquialDados, IReadOnlyList<AuditoriaParoquial>>(p, a => a.AuditoriaAsync(CancellationToken.None)); Assert.Equal(6, aud.Count(x => x.Modulo == "rifas"));
    }
}
