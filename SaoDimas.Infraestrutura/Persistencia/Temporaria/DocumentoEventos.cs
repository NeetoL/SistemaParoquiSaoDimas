namespace SaoDimas.Infraestrutura.Persistencia.Temporaria;

/// <summary>
/// Documento JSON guardado no navegador (persistência temporária do módulo de Eventos).
/// Estrutura compacta e equivalente às futuras tabelas: Evento, ProdutoEvento, LoteTicket, DistribuicaoTicket e
/// PrestacaoContas. Os tickets não são armazenados: são derivados dos lotes e das faixas.
/// </summary>
internal sealed class DocumentoEventos
{
    public const int VersaoAtual = 2;

    public int Versao { get; set; } = VersaoAtual;

    /// <summary>Próximos identificadores (o equivalente às colunas IDENTITY no SQL Server).</summary>
    public SequenciasRegistro Sequencias { get; set; } = new();

    public List<EventoRegistro> Eventos { get; set; } = [];
    public List<SaoDimas.Dominio.Entities.EventoBase> EventosBase { get; set; } = [];

    public List<LoteRegistro> Lotes { get; set; } = [];
}

internal sealed class SequenciasRegistro
{
    public int Evento { get; set; }

    public int Produto { get; set; }

    public int Lote { get; set; }

    public int Distribuicao { get; set; }
}

internal sealed class EventoRegistro
{
    public Guid EventoBaseId { get; set; }
    public SaoDimas.Dominio.Entities.EventoBase? Base { get; set; }
    public int Ano { get; set; }
    public SaoDimas.Dominio.Entities.EstadoCaixa? Caixa { get; set; }
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public int ComunidadeId { get; set; }

    public DateOnly DataInicio { get; set; }

    public DateOnly? DataFim { get; set; }

    public int Status { get; set; }

    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public List<ProdutoRegistro> Produtos { get; set; } = [];
}

internal sealed class ProdutoRegistro
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public decimal Preco { get; set; }

    public bool Ativo { get; set; }

    public int ProximoNumeroTicket { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }
}

internal sealed class LoteRegistro
{
    public List<SaoDimas.Dominio.Entities.CancelamentoTickets> Cancelamentos { get; set; } = [];
    public int Id { get; set; }

    public int EventoId { get; set; }

    public int ProdutoEventoId { get; set; }

    public int NumeroInicial { get; set; }

    public int NumeroFinal { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public List<DistribuicaoRegistro> Distribuicoes { get; set; } = [];
}

internal sealed class DistribuicaoRegistro
{
    public int Id { get; set; }

    public int NumeroInicial { get; set; }

    public int NumeroFinal { get; set; }

    public string? Responsavel { get; set; }

    public DateTime DataDistribuicao { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public int Status { get; set; }

    public PrestacaoRegistro? Prestacao { get; set; }
}

internal sealed class PrestacaoRegistro
{
    public int Recebidos { get; set; }

    public int Vendidos { get; set; }

    public int Devolvidos { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal ValorEntregue { get; set; }

    public string? Justificativa { get; set; }

    public DateTime RegistradaEm { get; set; }
}
