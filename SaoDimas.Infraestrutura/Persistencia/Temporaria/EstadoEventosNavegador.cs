using System.Text.Json;
using System.Text.Json.Serialization;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio.Entities;

namespace SaoDimas.Infraestrutura.Persistencia.Temporaria;

/// <summary>
/// Persistência TEMPORÁRIA do módulo de Eventos (uma instância por requisição). Recebe o documento guardado no
/// navegador, entrega aos repositórios as entidades reconstituídas e, ao salvar, gera o documento atualizado
/// que a apresentação devolve ao navegador. Será substituída por EF Core + SQL Server.
/// </summary>
internal sealed class EstadoEventosNavegador : IEstadoEventosNavegador
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private DocumentoEventos _documento = new();
    private readonly Dictionary<int, Evento> _eventos = [];
    private readonly Dictionary<int, LoteTicket> _lotes = [];
    private readonly List<Evento> _eventosNovos = [];
    private readonly List<LoteTicket> _lotesNovos = [];
    private string? _documentoAlterado;

    public bool Carregar(string? documentoJson)
    {
        if (string.IsNullOrWhiteSpace(documentoJson))
        {
            _documento = new DocumentoEventos();
            return true;
        }

        try
        {
            var documento = JsonSerializer.Deserialize<DocumentoEventos>(documentoJson, Json);
            if (documento is null || documento.Versao is not (1 or DocumentoEventos.VersaoAtual)
                || documento.Eventos is null || documento.Lotes is null || documento.Sequencias is null || documento.EventosBase is null
                || documento.Eventos.Any(e => e is null || e.Produtos is null)
                || documento.Lotes.Any(l => l is null || l.Distribuicoes is null))
            {
                return false;
            }

            _documento = documento;
            // Migração aditiva: não agrupa eventos legados pelo nome nem altera seus identificadores operacionais.
            foreach (var registro in _documento.Eventos)
            {
                if (registro.EventoBaseId != Guid.Empty && _documento.EventosBase.Any(b => b.Id == registro.EventoBaseId)) continue;
                var identidade = MapeadorEventos.ParaEvento(registro).Base;
                registro.EventoBaseId = identidade.Id;
                if (!_documento.EventosBase.Any(b => b.Id == identidade.Id)) _documento.EventosBase.Add(identidade);
            }
            _documento.Versao = DocumentoEventos.VersaoAtual;
            // Ao importar arquivos antigos, os próximos IDs nunca reutilizam registros existentes.
            _documento.Sequencias.Evento = Math.Max(_documento.Sequencias.Evento, _documento.Eventos.Select(e => e.Id).DefaultIfEmpty().Max());
            _documento.Sequencias.Produto = Math.Max(_documento.Sequencias.Produto, _documento.Eventos.SelectMany(e => e.Produtos).Select(p => p.Id).DefaultIfEmpty().Max());
            _documento.Sequencias.Lote = Math.Max(_documento.Sequencias.Lote, _documento.Lotes.Select(l => l.Id).DefaultIfEmpty().Max());
            _documento.Sequencias.Distribuicao = Math.Max(_documento.Sequencias.Distribuicao, _documento.Lotes.SelectMany(l => l.Distribuicoes).Select(d => d.Id).DefaultIfEmpty().Max());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public string? ObterDocumentoAlterado() => _documentoAlterado;

    internal Evento? ObterEvento(int id)
    {
        if (_eventos.TryGetValue(id, out var carregado))
        {
            return carregado;
        }

        var registro = _documento.Eventos.FirstOrDefault(evento => evento.Id == id);
        return registro is null ? null : _eventos[id] = MapeadorEventos.ParaEvento(registro,
            _documento.EventosBase.FirstOrDefault(b => b.Id == registro.EventoBaseId));
    }

    internal IReadOnlyList<Evento> ListarEventos() =>
        _documento.Eventos.Select(registro => ObterEvento(registro.Id)!).ToList();

    internal LoteTicket? ObterLote(int id)
    {
        if (_lotes.TryGetValue(id, out var carregado))
        {
            return carregado;
        }

        var registro = _documento.Lotes.FirstOrDefault(lote => lote.Id == id);
        return registro is null ? null : _lotes[id] = MapeadorEventos.ParaLote(registro);
    }

    internal IReadOnlyList<LoteTicket> ListarLotes(Func<LoteRegistro, bool> filtro) =>
        _documento.Lotes.Where(filtro).Select(registro => ObterLote(registro.Id)!).ToList();

    internal void Adicionar(Evento evento) => _eventosNovos.Add(evento);

    internal void Adicionar(LoteTicket lote) => _lotesNovos.Add(lote);

    /// <summary>
    /// Atribui identificadores aos novos registros e grava no documento tudo o que foi carregado ou adicionado
    /// (equivalente ao SaveChanges: uma única unidade de trabalho por requisição).
    /// </summary>
    internal void Salvar()
    {
        var sequencias = _documento.Sequencias;

        foreach (var evento in _eventosNovos)
        {
            MapeadorEventos.DefinirId(evento, ++sequencias.Evento);
            _eventos[evento.Id] = evento;
        }

        foreach (var evento in _eventos.Values)
        {
            var indiceBase = _documento.EventosBase.FindIndex(b => b.Id == evento.Base.Id);
            if (indiceBase < 0) _documento.EventosBase.Add(evento.Base);
            else _documento.EventosBase[indiceBase] = evento.Base;
            foreach (var produto in evento.Produtos.Where(produto => produto.Id == 0))
            {
                MapeadorEventos.DefinirId(produto, ++sequencias.Produto);
                MapeadorEventos.DefinirChaveEstrangeira(produto, nameof(ProdutoEvento.EventoId), evento.Id);
            }

            Substituir(_documento.Eventos, MapeadorEventos.ParaRegistro(evento), registro => registro.Id);
        }

        foreach (var lote in _lotesNovos)
        {
            MapeadorEventos.DefinirId(lote, ++sequencias.Lote);
            _lotes[lote.Id] = lote;
        }

        foreach (var lote in _lotes.Values)
        {
            foreach (var distribuicao in lote.Distribuicoes.Where(distribuicao => distribuicao.Id == 0))
            {
                MapeadorEventos.DefinirId(distribuicao, ++sequencias.Distribuicao);
                MapeadorEventos.DefinirChaveEstrangeira(distribuicao, nameof(DistribuicaoTicket.LoteTicketId), lote.Id);
            }

            Substituir(_documento.Lotes, MapeadorEventos.ParaRegistro(lote), registro => registro.Id);
        }

        _eventosNovos.Clear();
        _lotesNovos.Clear();
        _documentoAlterado = JsonSerializer.Serialize(_documento, Json);
    }

    private static void Substituir<T>(List<T> registros, T registro, Func<T, int> id)
    {
        var indice = registros.FindIndex(existente => id(existente) == id(registro));
        if (indice >= 0)
        {
            registros[indice] = registro;
        }
        else
        {
            registros.Add(registro);
        }
    }
}
