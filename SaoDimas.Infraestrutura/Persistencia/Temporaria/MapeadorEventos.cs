using System.Reflection;
using System.Runtime.CompilerServices;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Infraestrutura.Persistencia.Temporaria;

/// <summary>
/// Converte registros do documento em entidades e vice-versa. Assim como o EF Core, reconstitui as entidades
/// por reflexão (sem passar pelas fábricas), mantendo o domínio livre de código de persistência.
/// </summary>
internal static class MapeadorEventos
{
    public static Evento ParaEvento(EventoRegistro registro, EventoBase? eventoBase = null)
    {
        var evento = Novo<Evento>();
        // Identidade determinística para documentos anteriores, preservada ao salvar a primeira alteração.
        var bytes = new byte[16];
        BitConverter.GetBytes(registro.Id).CopyTo(bytes, 0);
        bytes[15] = 1;
        Definir(evento, nameof(Evento.Base), eventoBase ?? registro.Base ?? new EventoBase(new Guid(bytes), registro.Nome, registro.Descricao, registro.ComunidadeId, true));
        Definir(evento, nameof(Evento.Ano), registro.Ano == 0 ? registro.DataInicio.Year : registro.Ano);
        Definir(evento, nameof(Evento.Caixa), CaixaEvento.Reconstituir(registro.Caixa));
        Definir(evento, nameof(Evento.Id), registro.Id);
        Definir(evento, nameof(Evento.Nome), registro.Nome);
        Definir(evento, nameof(Evento.Descricao), registro.Descricao);
        Definir(evento, nameof(Evento.ComunidadeId), registro.ComunidadeId);
        Definir(evento, nameof(Evento.DataInicio), registro.DataInicio);
        Definir(evento, nameof(Evento.DataFim), registro.DataFim);
        Definir(evento, nameof(Evento.Status), (StatusEvento)registro.Status);
        Definir(evento, nameof(Evento.Observacoes), registro.Observacoes);
        Definir(evento, nameof(Evento.CriadoEm), registro.CriadoEm);
        Definir(evento, nameof(Evento.AtualizadoEm), registro.AtualizadoEm);
        DefinirCampo(evento, "_produtos", registro.Produtos.Select(produto => ParaProduto(registro.Id, produto)).ToList());
        return evento;
    }

    public static EventoRegistro ParaRegistro(Evento evento) => new()
    {
        EventoBaseId = evento.Base.Id,
        Ano = evento.Ano,
        Caixa = evento.Caixa.Exportar(),
        Id = evento.Id,
        Nome = evento.Nome,
        Descricao = evento.Descricao,
        ComunidadeId = evento.ComunidadeId,
        DataInicio = evento.DataInicio,
        DataFim = evento.DataFim,
        Status = (int)evento.Status,
        Observacoes = evento.Observacoes,
        CriadoEm = evento.CriadoEm,
        AtualizadoEm = evento.AtualizadoEm,
        Produtos = evento.Produtos.Select(produto => new ProdutoRegistro
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            Ativo = produto.Ativo,
            ProximoNumeroTicket = produto.ProximoNumeroTicket,
            CriadoEm = produto.CriadoEm,
            AtualizadoEm = produto.AtualizadoEm
        }).ToList()
    };

    public static LoteTicket ParaLote(LoteRegistro registro)
    {
        var lote = Novo<LoteTicket>();
        DefinirCampo(lote, "_cancelamentos", registro.Cancelamentos.ToList());
        Definir(lote, nameof(LoteTicket.Id), registro.Id);
        Definir(lote, nameof(LoteTicket.EventoId), registro.EventoId);
        Definir(lote, nameof(LoteTicket.ProdutoEventoId), registro.ProdutoEventoId);
        Definir(lote, nameof(LoteTicket.NumeroInicial), registro.NumeroInicial);
        Definir(lote, nameof(LoteTicket.NumeroFinal), registro.NumeroFinal);
        Definir(lote, nameof(LoteTicket.PrecoUnitario), registro.PrecoUnitario);
        Definir(lote, nameof(LoteTicket.CriadoEm), registro.CriadoEm);
        Definir(lote, nameof(LoteTicket.AtualizadoEm), registro.AtualizadoEm);
        DefinirCampo(lote, "_distribuicoes", registro.Distribuicoes.Select(distribuicao => ParaDistribuicao(registro.Id, distribuicao)).ToList());
        return lote;
    }

    public static LoteRegistro ParaRegistro(LoteTicket lote) => new()
    {
        Cancelamentos = lote.Cancelamentos.ToList(),
        Id = lote.Id,
        EventoId = lote.EventoId,
        ProdutoEventoId = lote.ProdutoEventoId,
        NumeroInicial = lote.NumeroInicial,
        NumeroFinal = lote.NumeroFinal,
        PrecoUnitario = lote.PrecoUnitario,
        CriadoEm = lote.CriadoEm,
        AtualizadoEm = lote.AtualizadoEm,
        Distribuicoes = lote.Distribuicoes.Select(distribuicao => new DistribuicaoRegistro
        {
            Id = distribuicao.Id,
            NumeroInicial = distribuicao.NumeroInicial,
            NumeroFinal = distribuicao.NumeroFinal,
            Responsavel = distribuicao.Responsavel?.Nome,
            DataDistribuicao = distribuicao.DataDistribuicao,
            AtualizadoEm = distribuicao.AtualizadoEm,
            Status = (int)distribuicao.Status,
            Prestacao = distribuicao.Prestacao is { } prestacao
                ? new PrestacaoRegistro
                {
                    Recebidos = prestacao.Recebidos,
                    Vendidos = prestacao.Vendidos,
                    Devolvidos = prestacao.Devolvidos,
                    PrecoUnitario = prestacao.PrecoUnitario,
                    ValorEntregue = prestacao.ValorEntregue,
                    Justificativa = prestacao.Justificativa,
                    RegistradaEm = prestacao.RegistradaEm
                }
                : null
        }).ToList()
    };

    /// <summary>Atribui o Id gerado pela persistência (equivalente ao IDENTITY do banco).</summary>
    public static void DefinirId(object entidade, int id) => Definir(entidade, "Id", id);

    public static void DefinirChaveEstrangeira(object entidade, string propriedade, int id) => Definir(entidade, propriedade, id);

    private static ProdutoEvento ParaProduto(int eventoId, ProdutoRegistro registro)
    {
        var produto = Novo<ProdutoEvento>();
        Definir(produto, nameof(ProdutoEvento.Id), registro.Id);
        Definir(produto, nameof(ProdutoEvento.EventoId), eventoId);
        Definir(produto, nameof(ProdutoEvento.Nome), registro.Nome);
        Definir(produto, nameof(ProdutoEvento.Descricao), registro.Descricao);
        Definir(produto, nameof(ProdutoEvento.Preco), registro.Preco);
        Definir(produto, nameof(ProdutoEvento.Ativo), registro.Ativo);
        Definir(produto, nameof(ProdutoEvento.ProximoNumeroTicket), registro.ProximoNumeroTicket);
        Definir(produto, nameof(ProdutoEvento.CriadoEm), registro.CriadoEm);
        Definir(produto, nameof(ProdutoEvento.AtualizadoEm), registro.AtualizadoEm);
        return produto;
    }

    private static DistribuicaoTicket ParaDistribuicao(int loteId, DistribuicaoRegistro registro)
    {
        var distribuicao = Novo<DistribuicaoTicket>();
        Definir(distribuicao, nameof(DistribuicaoTicket.Id), registro.Id);
        Definir(distribuicao, nameof(DistribuicaoTicket.LoteTicketId), loteId);
        Definir(distribuicao, nameof(DistribuicaoTicket.NumeroInicial), registro.NumeroInicial);
        Definir(distribuicao, nameof(DistribuicaoTicket.NumeroFinal), registro.NumeroFinal);
        Definir(distribuicao, nameof(DistribuicaoTicket.Responsavel),
            string.IsNullOrWhiteSpace(registro.Responsavel) ? null : Responsavel.Reconstituir(registro.Responsavel));
        Definir(distribuicao, nameof(DistribuicaoTicket.DataDistribuicao), registro.DataDistribuicao);
        Definir(distribuicao, nameof(DistribuicaoTicket.AtualizadoEm), registro.AtualizadoEm);
        Definir(distribuicao, nameof(DistribuicaoTicket.Status), (StatusDistribuicao)registro.Status);
        Definir(distribuicao, nameof(DistribuicaoTicket.Prestacao), registro.Prestacao is null ? null : ParaPrestacao(registro.Prestacao));
        return distribuicao;
    }

    private static PrestacaoContas ParaPrestacao(PrestacaoRegistro registro)
    {
        var prestacao = Novo<PrestacaoContas>();
        Definir(prestacao, nameof(PrestacaoContas.Recebidos), registro.Recebidos);
        Definir(prestacao, nameof(PrestacaoContas.Vendidos), registro.Vendidos);
        Definir(prestacao, nameof(PrestacaoContas.Devolvidos), registro.Devolvidos);
        Definir(prestacao, nameof(PrestacaoContas.PrecoUnitario), registro.PrecoUnitario);
        Definir(prestacao, nameof(PrestacaoContas.ValorEntregue), registro.ValorEntregue);
        Definir(prestacao, nameof(PrestacaoContas.Justificativa), registro.Justificativa);
        Definir(prestacao, nameof(PrestacaoContas.RegistradaEm), registro.RegistradaEm);
        return prestacao;
    }

    private static T Novo<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void Definir(object entidade, string propriedade, object? valor) =>
        (entidade.GetType().GetProperty(propriedade, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{entidade.GetType().Name}.{propriedade} não encontrada."))
        .SetValue(entidade, valor);

    private static void DefinirCampo(object entidade, string campo, object valor) =>
        (entidade.GetType().GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{entidade.GetType().Name}.{campo} não encontrado."))
        .SetValue(entidade, valor);
}
