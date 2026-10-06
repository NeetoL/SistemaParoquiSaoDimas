using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SaoDimas.Aplicacao.Configuracoes;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio.Entities;

namespace SaoDimas.Infraestrutura.Services.Implementation;

internal sealed class RelatorioFechamentoService(IOptions<ConfiguracaoParoquia> opcoes, IHostEnvironment ambiente) : IRelatorioFechamentoService
{
    public byte[] Gerar(GestaoEventoDto dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        if (dados.Caixa.Fechamento is null || dados.Caixa.Conferencia is null || dados.Caixa.Abertura is null)
            throw new InvalidOperationException("O relatório exige caixa conferido e fechado.");
        var ab = dados.Caixa.Abertura; var cf = dados.Caixa.Conferencia; var fechamento = dados.Caixa.Fechamento;
        var identidade = fechamento.Identidade ?? new IdentidadeFechamento(dados.Evento.Nome, dados.Ano, dados.Comunidade, dados.Inicio, dados.Fim);
        string Reais(decimal valor) => valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
        string Data(DateTimeOffset valor) => valor.ToOffset(TimeSpan.FromHours(-3)).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(32); page.DefaultTextStyle(s => s.FontSize(10).FontColor("#1B201D"));
            page.Header().ShowOnce().PaddingBottom(16).Row(row =>
            {
                row.ConstantItem(50).Image(Path.Combine(ambiente.ContentRootPath, opcoes.Value.CaminhoLogoImpressao));
                row.RelativeItem().PaddingLeft(12).Column(col =>
                {
                    col.Item().Text(opcoes.Value.Nome).FontSize(16).Bold().FontColor("#17633A");
                    col.Item().Text("Relatório de fechamento de evento").FontSize(12);
                    col.Item().Text($"{identidade.Evento} · Edição {identidade.Ano}").Bold();
                    col.Item().Text($"{identidade.Comunidade} · {identidade.Inicio:dd/MM/yyyy} — {(identidade.Fim ?? identidade.Inicio):dd/MM/yyyy}");
                });
            });
            page.Content().Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("Abertura e fechamento").FontSize(12).Bold();
                col.Item().Text($"Abertura: {Data(ab.Data)} · {ab.Operador}\nFundo de troco: {Reais(ab.FundoTroco)} (não compõe a receita)\nObservação: {ab.Observacao}\nFechamento: {Data(fechamento.Data)} · {fechamento.Operador}");
                col.Item().Text("Resumo financeiro").FontSize(12).Bold();
                col.Item().Text($"Potencial: {Reais(dados.Potencial)}\nDevido pelas vendas: {Reais(dados.Devido)}\nRecebido: {Reais(dados.Prestado)}\nSaldo a receber: {Reais(dados.Pendente)}");
                foreach (var forma in Enum.GetValues<FormaRecebimento>()) col.Item().Text($"{forma}: {Reais(dados.Total(forma))}");
                col.Item().Text($"Dinheiro teórico: {Reais(cf.Teorico)}\nDinheiro contado: {Reais(cf.Contado)}\nDiferença: {Reais(cf.Contado - cf.Teorico)}\nConferência: {Data(cf.Data)} · {cf.Operador}\nJustificativa: {cf.Justificativa}");
                col.Item().Text("Resumo dos tickets e resultado por produto").FontSize(12).Bold();
                col.Item().Text($"Gerados: {dados.Produtos.Sum(p => p.Gerados)} · Distribuídos: {dados.Responsaveis.Sum(r => r.Faixas.Sum(f => f.Recebidos))} · Vendidos: {dados.Produtos.Sum(p => p.Vendidos)} · Devolvidos: {dados.Produtos.Sum(p => p.Devolvidos)} · Cancelados: {dados.Produtos.Sum(p => p.Cancelados)}");
                foreach (var produto in dados.Produtos)
                    col.Item().Text($"{produto.Nome}: {produto.Gerados} gerados, {produto.Vendidos} vendidos, {produto.Devolvidos} devolvidos, {produto.Cancelados} cancelados · {Reais(produto.Valor)}");
                col.Item().Text("Resultado por responsável").FontSize(12).Bold();
                foreach (var responsavel in dados.Responsaveis)
                {
                    col.Item().Text($"{responsavel.Nome} · {responsavel.Situacao}\nDevido {Reais(responsavel.Devido)} · Prestado {Reais(responsavel.Prestado)} · Saldo {Reais(responsavel.Pendente)}").Bold();
                    foreach (var faixa in responsavel.Faixas)
                        col.Item().Text($"{faixa.Produto} · {faixa.Faixa} · {faixa.Recebidos} recebidos · {faixa.Vendidos?.ToString(CultureInfo.InvariantCulture) ?? "não conferido"} vendidos · {faixa.Devolvidos?.ToString(CultureInfo.InvariantCulture) ?? "não conferido"} devolvidos · preço {Reais(faixa.Preco)}");
                }
                col.Item().Text("Situação final: " + (fechamento.Pendencias.Count == 0 ? "Fechado sem pendências" : "Fechado excepcionalmente com pendências")).FontSize(12).Bold();
                foreach (var pendencia in fechamento.Pendencias) col.Item().Text(pendencia);
                col.Item().Text($"Motivo: {fechamento.Motivo}\nJustificativa administrativa: {fechamento.Justificativa}");
                col.Item().Text("Livro de recebimentos e estornos").FontSize(12).Bold();
                foreach (var mov in dados.Caixa.Movimentacoes.OrderBy(m => m.Data))
                    col.Item().Text($"{Data(mov.Data)} · {mov.Responsavel} · {Reais(mov.Valor)} · {mov.Forma}\n{mov.Operador} · {mov.Observacao}\nRegistro: {mov.Id}" + (mov.OriginalId is { } original ? $" · Estorno de {original}" : ""));
                col.Item().PageBreak();
                col.Item().Text("Auditoria").FontSize(12).Bold();
                foreach (var audit in dados.Caixa.Historico.OrderBy(a => a.Data))
                    col.Item().Text($"{Data(audit.Data)} · {audit.Operador} · {audit.Acao}\n{audit.Antes} → {audit.Depois}");
            });
            page.Footer().AlignCenter().Text(t => { t.Span("Paróquia São Dimas · "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
        })).GeneratePdf();
    }

}
