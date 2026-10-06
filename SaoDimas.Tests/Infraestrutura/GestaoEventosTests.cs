using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using UglyToad.PdfPig;

namespace SaoDimas.Tests.Infraestrutura;

public sealed partial class EventosPersistenciaTemporariaTests
{
    [Fact]
    public async Task Vendas_parciais_permitem_prestacao_sem_tratar_tickets_em_posse_como_devolvidos()
    {
        var (id, _, lote) = await PrepararGestao();
        var faixa = (await Painel(id))!.Responsaveis.Single(r => r.Nome == "João da Silva").Faixas[0].Id;
        Assert.True((await Gestao(id, app => app.SituacaoAsync(id, lote, faixa, 20, 0, "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.AbrirAsync(id, 300m, "José", null, Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.ReceberAsync(id, "João da Silva", 200m, FormaRecebimento.Dinheiro, "José", null, Ct))).Sucesso);
        var parcial = (await Painel(id))!.Responsaveis.Single(r => r.Nome == "João da Silva");
        Assert.Equal(500m, parcial.Devido); Assert.Equal(300m, parcial.Pendente); Assert.Equal("Parcial", parcial.Situacao);
        Assert.False(parcial.TicketsConferidos); Assert.Equal(0, parcial.Faixas[0].Devolvidos);
        Assert.False((await Gestao(id, app => app.SituacaoAsync(id, lote, faixa, 19, 0, "José", Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.SituacaoAsync(id, lote, faixa, 42, 9, "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.SituacaoAsync(id, lote, faixa, 42, 8, "José", Ct))).Sucesso);
        var final = (await Painel(id))!;
        var joao = final.Responsaveis.Single(r => r.Nome == "João da Silva");
        Assert.Equal(1050m, joao.Devido); Assert.Equal(850m, joao.Pendente); Assert.True(joao.TicketsConferidos);
        Assert.Equal(2, final.Caixa.Historico.Count(h => h.Acao == "Venda / devolução"));
        Assert.Contains(final.Caixa.Historico, h => h.Acao == "Venda / devolução" && h.Antes.Contains("20 vendidos", StringComparison.Ordinal));
    }
    [Fact]
    public async Task Cancelamento_preserva_numeros_remove_pendencia_e_impede_distribuicao_e_impressao()
    {
        var (id, produto, _) = await PrepararGestao();
        var novoLote = (await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(id, produto, 50, Ct))).Valor;
        Assert.Contains((await Painel(id))!.Disponiveis, f => f.Inicial == 63 && f.Final == 112);
        Assert.True((await Gestao(id, app => app.CancelarTicketsAsync(id, novoLote, 63, 112, "Sobras não utilizadas", "José", Ct))).Sucesso);
        var painel = (await Painel(id))!;
        Assert.Equal(50, painel.Produtos[0].Cancelados); Assert.Empty(painel.Disponiveis);
        Assert.Contains(painel.Caixa.Historico, h => h.Acao == "Cancelamento de tickets");
        Assert.False((await Requisicao<ITicketAplicacao, Resultado>(app => app.DistribuirAsync(id, novoLote, new(63, 70, "Carlos"), Ct))).Sucesso);
        Assert.False((await Requisicao<ITicketAplicacao, Resultado<ArquivoPdf>>(app => app.GerarPdfAsync(id, novoLote, null, Ct))).Sucesso);
        var proximo = (await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(id, produto, 1, Ct))).Valor;
        Assert.Equal("113–113", (await Requisicao<ITicketAplicacao, LoteDetalhesDto?>(app => app.ObterLoteAsync(id, proximo, Ct)))!.Faixa);
    }

    [Fact]
    public async Task Versao_um_migra_sem_perder_identificadores_produtos_lotes_e_faixas()
    {
        var (id, produto, lote) = await PrepararGestao();
        var json = System.Text.Json.Nodes.JsonNode.Parse(_documentoNoNavegador!)!.AsObject();
        json["versao"] = 1; json.Remove("eventosBase");
        foreach (var evento in json["eventos"]!.AsArray())
        { evento!.AsObject().Remove("eventoBaseId"); evento.AsObject().Remove("caixa"); evento.AsObject().Remove("ano"); }
        _documentoNoNavegador = json.ToJsonString();
        var primeiro = (await Painel(id))!; var segundo = (await Painel(id))!;
        Assert.Equal(primeiro.Evento.Id, segundo.Evento.Id);
        Assert.Equal(2, primeiro.Responsaveis.Count); Assert.Equal(62, primeiro.Produtos[0].Gerados);
        Assert.True((await Gestao(id, app => app.AbrirAsync(id, 300m, "José", null, Ct))).Sucesso);
        Assert.Contains("\"versao\":2", _documentoNoNavegador!, StringComparison.Ordinal);
        var detalhes = (await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(app => app.ObterDetalhesAsync(id, Ct)))!;
        Assert.Equal(produto, detalhes.Produtos[0].Id); Assert.Equal(lote, detalhes.Lotes[0].Id); Assert.Equal(25m, detalhes.Produtos[0].Preco);
    }

    [Fact]
    public async Task Novo_evento_pode_usar_modelo_sem_compartilhar_identidade_ou_financeiro()
    {
        var (id, _, _) = await PrepararGestao();
        var novo = (await Requisicao<IEventoAplicacao, Resultado<int>>(app => app.CriarAsync(new("Almoço Paroquial", null, 2,
            new(2026, 10, 20), null, null, id), Ct))).Valor;
        var painel = (await Painel(novo))!;
        Assert.NotEqual((await Painel(id))!.Evento.Id, painel.Evento.Id);
        Assert.Equal("Almoço Paroquial", painel.Evento.Nome); Assert.Empty(painel.Responsaveis);
        Assert.Null(painel.Caixa.Abertura); Assert.Single(painel.Produtos); Assert.Equal(0, painel.Produtos[0].Gerados);
    }
    private static readonly CancellationToken Ct = CancellationToken.None;
    private Task<Resultado> Gestao(int id, Func<IGestaoEventoAplicacao, Task<Resultado>> acao) => Requisicao<IGestaoEventoAplicacao, Resultado>(acao);
    private Task<GestaoEventoDto?> Painel(int id) => Requisicao<IGestaoEventoAplicacao, GestaoEventoDto?>(app => app.ObterAsync(id, Ct));

    private async Task<(int Evento, int Produto, int Lote)> PrepararGestao()
    {
        var id = (await Requisicao<IEventoAplicacao, Resultado<int>>(app => app.CriarAsync(new("Festa de Santa Teresinha", null, 2,
            new(2026, 9, 19), new(2026, 9, 27), null), Ct))).Valor;
        var produto = (await Requisicao<IEventoAplicacao, Resultado<int>>(app => app.AdicionarProdutoAsync(id, new("Feijoada", null, 25m, true), Ct))).Valor;
        var lote = (await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(id, produto, 62, Ct))).Valor;
        Assert.True((await Requisicao<ITicketAplicacao, Resultado>(app => app.DistribuirAsync(id, lote, new(1, 50, "João da Silva"), Ct))).Sucesso);
        Assert.True((await Requisicao<ITicketAplicacao, Resultado>(app => app.DistribuirAsync(id, lote, new(51, 62, "Maria"), Ct))).Sucesso);
        return (id, produto, lote);
    }

    [Fact]
    public async Task Prestacoes_parciais_estorno_conferencia_fechamento_e_edicoes_independentes()
    {
        var (id, produto, lote) = await PrepararGestao();
        var painel = (await Painel(id))!;
        var joao = painel.Responsaveis.Single(r => r.Nome == "João da Silva");
        var maria = painel.Responsaveis.Single(r => r.Nome == "Maria");
        Assert.True((await Gestao(id, app => app.SituacaoAsync(id, lote, joao.Faixas[0].Id, 42, 8, "Caixa", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.SituacaoAsync(id, lote, maria.Faixas[0].Id, 12, 0, "Caixa", Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.ReceberAsync(id, joao.Nome, 500m, FormaRecebimento.Dinheiro, "José", null, Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.AbrirAsync(id, 300m, "José", "Troco inicial", Ct))).Sucesso);
        foreach (var (valor, forma, acumulado, pendente) in new[]
        {
            (500m, FormaRecebimento.Dinheiro, 500m, 550m),
            (300m, FormaRecebimento.Pix, 800m, 250m),
            (250m, FormaRecebimento.Transferencia, 1050m, 0m)
        })
        {
            Assert.True((await Gestao(id, app => app.ReceberAsync(id, joao.Nome, valor, forma, "José", null, Ct))).Sucesso);
            var atual = (await Painel(id))!.Responsaveis.Single(r => r.Nome == joao.Nome);
            Assert.Equal(1050m, atual.Devido); Assert.Equal(acumulado, atual.Prestado); Assert.Equal(pendente, atual.Pendente);
            Assert.Equal(pendente == 0m ? "Quitado" : "Parcial", atual.Situacao);
        }
        painel = (await Painel(id))!;
        Assert.Equal(1550m, painel.Potencial); Assert.Equal(1350m, painel.Devido); Assert.Equal(1050m, painel.Prestado);
        Assert.Equal(800m, painel.DinheiroTeorico); Assert.Equal(300m, painel.Pendente);
        Assert.True((await Gestao(id, app => app.ConferirAsync(id, 800m, null, "José", Ct))).Sucesso);
        var fechamentoPendente = await Gestao(id, app => app.FecharAsync(id, false, null, null, "José", Ct));
        Assert.False(fechamentoPendente.Sucesso); Assert.Contains("Maria", fechamentoPendente.Erros[0].Mensagem, StringComparison.Ordinal);
        Assert.True((await Gestao(id, app => app.ReceberAsync(id, maria.Nome, 300m, FormaRecebimento.Dinheiro, "José", null, Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.FecharAsync(id, false, null, null, "José", Ct))).Sucesso);
        painel = (await Painel(id))!;
        var original = painel.Caixa.Movimentacoes.First(m => m.Valor == 500m);
        Assert.True((await Gestao(id, app => app.EstornarAsync(id, original.Id, "Valor lançado incorretamente", "José", Ct))).Sucesso);
        painel = (await Painel(id))!;
        Assert.Contains(painel.Caixa.Movimentacoes, m => m.Id == original.Id);
        Assert.Contains(painel.Caixa.Movimentacoes, m => m.OriginalId == original.Id && m.Valor == -500m);
        Assert.Equal(500m, painel.Pendente);
        Assert.False((await Gestao(id, app => app.EstornarAsync(id, original.Id, "Segundo estorno", "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.ReceberAsync(id, joao.Nome, 500m, FormaRecebimento.Dinheiro, "José", "Lançamento corrigido", Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.ConferirAsync(id, 1050m, null, "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.ConferirAsync(id, 1050m, "Diferença identificada na contagem", "José", Ct))).Sucesso);
        Assert.Equal(-50m, (await Painel(id))!.Caixa.Conferencia!.Contado - (await Painel(id))!.Caixa.Conferencia!.Teorico);
        Assert.True((await Gestao(id, app => app.ConferirAsync(id, 1100m, null, "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.FecharAsync(id, false, null, null, "José", Ct))).Sucesso);
        painel = (await Painel(id))!;
        Assert.Equal(SituacaoCaixa.Fechado, painel.SituacaoCaixa); Assert.Empty(painel.Pendencias);
        Assert.Equal(1350m, painel.Prestado); Assert.Equal(0m, painel.Pendente);
        Assert.False((await Requisicao<IEventoAplicacao, Resultado>(app => app.AtualizarProdutoAsync(id, produto, new("Feijoada", null, 50m, true), Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.EstornarAsync(id, original.Id, "Tentativa após fechar", "José", Ct))).Sucesso);
        Assert.False((await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(id, produto, 2, Ct))).Sucesso);

        var novoId = (await Requisicao<IGestaoEventoAplicacao, Resultado<int>>(app => app.NovaEdicaoAsync(id, 2027, new(2027, 9, 19), null, true, true, Ct))).Valor;
        var nova = (await Painel(novoId))!;
        Assert.Equal(painel.Evento.Id, nova.Evento.Id); Assert.Empty(nova.Responsaveis); Assert.Empty(nova.Caixa.Movimentacoes);
        Assert.Null(nova.Caixa.Abertura); Assert.Null(nova.Caixa.Fechamento); Assert.Equal(0m, nova.Prestado); Assert.Equal(0, nova.Produtos[0].Gerados);
        var detalhes = (await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(app => app.ObterDetalhesAsync(novoId, Ct)))!;
        Assert.Equal(25m, detalhes.Produtos[0].Preco);
        Assert.True((await Requisicao<IEventoAplicacao, Resultado>(app => app.AtualizarProdutoAsync(novoId, detalhes.Produtos[0].Id, new("Feijoada", null, 30m, true), Ct))).Sucesso);
        Assert.Equal(25m, (await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(app => app.ObterDetalhesAsync(id, Ct)))!.Produtos[0].Preco);
        Assert.False((await Requisicao<IGestaoEventoAplicacao, Resultado<int>>(app => app.NovaEdicaoAsync(id, 2027, new(2027, 9, 19), null, true, true, Ct))).Sucesso);
        Assert.False((await Gestao(novoId, app => app.SituacaoAsync(novoId, lote, joao.Faixas[0].Id, 42, 8, "José", Ct))).Sucesso);
        var pdf = await Requisicao<IRelatorioFechamentoService, byte[]>(app => Task.FromResult(app.Gerar(painel)));
        using var arquivo = PdfDocument.Open(pdf);
        var texto = string.Join(" ", arquivo.GetPages().Select(p => p.Text));
        Assert.Contains("São Dimas", texto, StringComparison.Ordinal); Assert.Contains("Estorno", texto, StringComparison.Ordinal);
        Assert.Contains("1.350,00", texto, StringComparison.Ordinal); Assert.All(arquivo.GetPages(), p => Assert.InRange(p.Width, 595, 596));
    }

    [Fact]
    public async Task Fechamento_excepcional_preserva_pendencias_e_exige_motivo()
    {
        var (id, _, _) = await PrepararGestao();
        Assert.True((await Gestao(id, app => app.AbrirAsync(id, 300m, "José", null, Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.ConferirAsync(id, 300m, null, "José", Ct))).Sucesso);
        Assert.False((await Gestao(id, app => app.FecharAsync(id, true, null, null, "José", Ct))).Sucesso);
        Assert.True((await Gestao(id, app => app.FecharAsync(id, true, "Evento encerrado administrativamente", "Tickets serão apurados pela coordenação", "Coordenador", Ct))).Sucesso);
        var painel = (await Painel(id))!;
        Assert.NotEmpty(painel.Caixa.Fechamento!.Pendencias);
        Assert.Contains(painel.Caixa.Historico, h => h.Acao == "Fechamento com pendências");
    }

    [Fact]
    public async Task Modelo_sem_precos_cria_produtos_inativos_e_edicao_vazia_nao_copia_produtos()
    {
        var (id, _, _) = await PrepararGestao();
        var semPreco = (await Requisicao<IGestaoEventoAplicacao, Resultado<int>>(app => app.NovaEdicaoAsync(id, 2027, new(2027, 9, 19), null, true, false, Ct))).Valor;
        var detalhes = (await Requisicao<IEventoAplicacao, EventoDetalhesDto?>(app => app.ObterDetalhesAsync(semPreco, Ct)))!;
        Assert.Equal(0m, detalhes.Produtos[0].Preco); Assert.False(detalhes.Produtos[0].Ativo);
        Assert.False((await Requisicao<ITicketAplicacao, Resultado<int>>(app => app.GerarLoteAsync(semPreco, detalhes.Produtos[0].Id, 50, Ct))).Sucesso);
        var vazia = (await Requisicao<IGestaoEventoAplicacao, Resultado<int>>(app => app.NovaEdicaoAsync(id, 2028, new(2028, 9, 19), null, false, false, Ct))).Valor;
        Assert.Empty((await Painel(vazia))!.Produtos);
    }
}
