using System.Globalization;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.Dominio;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.Repositories.Interface;

namespace SaoDimas.Aplicacao.Services.Implementation;

internal sealed class EventoAplicacao(
    IEventoRepositorio eventos,
    ILoteTicketRepositorio lotes,
    IComunidadeRepositorio comunidades,
    TimeProvider relogio) : IEventoAplicacao
{
    private static readonly Erro EventoNaoEncontrado = Erro.NaoEncontrado("Evento não encontrado.");

    public async Task<PainelEventosDto> ObterPainelAsync(FiltroEventos filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        filtro = filtro.Normalizado();

        var todos = await eventos.ListarAsync(cancellationToken);
        var todosLotes = await lotes.ListarTodosAsync(cancellationToken);
        var nomes = await NomesDasComunidadesAsync(cancellationToken);

        var indicadores = new IndicadoresEventosDto(
            todos.Count(evento => evento.Status == StatusEvento.Ativo),
            todos.Sum(evento => evento.Produtos.Count),
            todosLotes.Sum(lote => lote.Quantidade),
            todosLotes.Sum(lote => lote.QuantidadeDistribuida));

        var filtrados = todos
            .Where(evento => filtro.Busca is null || ContemSemAcento(evento.Nome, filtro.Busca))
            .Where(evento => filtro.ComunidadeId is null || evento.ComunidadeId == filtro.ComunidadeId)
            .Where(evento => filtro.Status is null || evento.Status == filtro.Status)
            .Where(evento => filtro.De is null || (evento.DataFim ?? evento.DataInicio) >= filtro.De)
            .Where(evento => filtro.Ate is null || evento.DataInicio <= filtro.Ate)
            .OrderByDescending(evento => evento.DataInicio)
            .ThenBy(evento => evento.Nome, StringComparer.CurrentCulture)
            .Select(evento => new EventoResumoDto(
                evento.Id, evento.Nome, nomes.GetValueOrDefault(evento.ComunidadeId, "—"), evento.DataInicio, evento.DataFim,
                evento.Produtos.Count, evento.Status, evento.PermiteAlteracoes)
                { EventoBaseId = evento.Base.Id, NomeBase = evento.Base.Nome, Ano = evento.Ano, TotalEdicoes = todos.Count(e => e.Base.Id == evento.Base.Id) })
            .ToList();

        return new PainelEventosDto(indicadores, filtrados);
    }

    public async Task<EventoDetalhesDto?> ObterDetalhesAsync(int eventoId, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return null;
        }

        var lotesDoEvento = await lotes.ListarPorEventoAsync(eventoId, cancellationToken);
        var nomes = await NomesDasComunidadesAsync(cancellationToken);
        string Produto(int produtoId) => evento.ObterProduto(produtoId)?.Nome ?? "—";

        var produtos = evento.Produtos
            .OrderBy(produto => produto.Nome, StringComparer.CurrentCulture)
            .Select(produto => new ProdutoEventoDto(
                produto.Id, produto.Nome, produto.Descricao, produto.Preco, produto.Ativo, produto.TicketsGerados, produto.ProximoNumeroTicket))
            .ToList();

        var resumoLotes = lotesDoEvento
            .OrderBy(lote => Produto(lote.ProdutoEventoId), StringComparer.CurrentCulture)
            .ThenBy(lote => lote.NumeroInicial)
            .Select(lote => new LoteResumoDto(
                lote.Id, lote.ProdutoEventoId, Produto(lote.ProdutoEventoId), lote.Faixa.ToString(), lote.Quantidade, lote.PrecoUnitario,
                lote.ValorPotencial, lote.QuantidadeDistribuida, lote.QuantidadeDisponivel, lote.TotalResponsaveis))
            .ToList();

        var faixasComResponsavel = lotesDoEvento
            .SelectMany(lote => lote.Distribuicoes
                .Where(distribuicao => distribuicao.PossuiResponsavel)
                .Select(distribuicao => (Lote: lote, Distribuicao: distribuicao)))
            .OrderBy(item => Produto(item.Lote.ProdutoEventoId), StringComparer.CurrentCulture)
            .ThenBy(item => item.Distribuicao.NumeroInicial)
            .ToList();

        // Uma pessoa pode ter várias faixas (em um ou mais produtos): agrupa pelo nome.
        var responsaveis = faixasComResponsavel
            .GroupBy(item => item.Distribuicao.Responsavel!.Nome, StringComparer.OrdinalIgnoreCase)
            .OrderBy(grupo => grupo.Key, StringComparer.CurrentCulture)
            .Select(grupo => new ResponsavelResumoDto(
                grupo.First().Distribuicao.Responsavel!.Nome,
                grupo.Select(item => $"{Produto(item.Lote.ProdutoEventoId)} {item.Distribuicao.Faixa}").ToList(),
                grupo.Sum(item => item.Distribuicao.Quantidade),
                grupo.Count(item => item.Distribuicao.Prestacao is null || item.Distribuicao.Prestacao.Vendidos + item.Distribuicao.Prestacao.Devolvidos < item.Distribuicao.Quantidade) +
                (grupo.Sum(item => item.Distribuicao.Prestacao?.ValorEsperado ?? 0m) >
                    (evento.Caixa.Abertura is null ? grupo.Sum(item => item.Distribuicao.Prestacao?.ValorEntregue ?? 0m) : evento.Caixa.Prestado(grupo.Key)) ? 1 : 0)))
            .ToList();

        var prestacoes = faixasComResponsavel
            .Select(item => new PrestacaoResumoDto(
                item.Lote.Id, item.Distribuicao.Id, Produto(item.Lote.ProdutoEventoId), item.Distribuicao.Faixa.ToString(),
                item.Distribuicao.Responsavel!.Nome, item.Distribuicao.Quantidade, item.Lote.PrecoUnitario,
                Mapeamento.Prestacao(item.Distribuicao.Prestacao)))
            .ToList();

        var registradas = prestacoes.Select(prestacao => prestacao.Prestacao).OfType<PrestacaoDto>().ToList();
        var recebido = evento.Caixa.Abertura is null ? registradas.Sum(p => p.ValorEntregue) : evento.Caixa.Receita;
        var totais = new TotaisPrestacaoDto(
            registradas.Sum(prestacao => prestacao.ValorEsperado),
            recebido,
            recebido - registradas.Sum(prestacao => prestacao.ValorEsperado),
            registradas.Count,
            prestacoes.Count - registradas.Count);

        var gerados = lotesDoEvento.Sum(lote => lote.Quantidade);
        var distribuidos = lotesDoEvento.Sum(lote => lote.QuantidadeDistribuida);

        return new EventoDetalhesDto(
            evento.Id, evento.Nome, evento.Descricao, evento.ComunidadeId, nomes.GetValueOrDefault(evento.ComunidadeId, "—"),
            evento.DataInicio, evento.DataFim, evento.Status, evento.Observacoes, evento.PermiteAlteracoes, evento.PermitePrestacaoDeContas,
            gerados, distribuidos, lotesDoEvento.Sum(l => l.QuantidadeDisponivel), produtos, resumoLotes, responsaveis, prestacoes, totais);
    }

    public async Task<DadosEvento?> ObterParaEdicaoAsync(int eventoId, CancellationToken cancellationToken)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);

        return evento is null
            ? null
            : new DadosEvento(evento.Nome, evento.Descricao, evento.ComunidadeId, evento.DataInicio, evento.DataFim, evento.Observacoes);
    }

    public async Task<Resultado<int>> CriarAsync(DadosEvento dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var comunidade = await comunidades.ObterPorIdAsync(dados.ComunidadeId, cancellationToken);
        if (comunidade is null)
        {
            return Resultado<int>.Falha(ComunidadeInvalida());
        }

        var resultado = Evento.Criar(
            dados.Nome, dados.Descricao, comunidade, dados.DataInicio, dados.DataFim, dados.Observacoes, relogio.GetUtcNow());
        if (!resultado.Sucesso)
        {
            return Resultado<int>.Falha(resultado.Erros);
        }

        if (dados.ModeloEdicaoId is int modeloId)
        {
            var modelo = await eventos.ObterPorIdAsync(modeloId, cancellationToken);
            if (modelo is null) return Resultado<int>.Falha(new Erro("ModeloEdicaoId", "Edição modelo não encontrada."));
            var copia = resultado.Valor.AplicarModelo(modelo.ExportarModelo(), true, relogio.GetUtcNow());
            if (!copia.Sucesso) return Resultado<int>.Falha(copia.Erros);
        }
        resultado.Valor.Caixa.Auditar(dados.Operador ?? "Sistema", "Cadastro de evento", "Novo evento",
            $"Evento-base {resultado.Valor.Base.Id}; edição {resultado.Valor.Ano}", relogio.GetUtcNow());
        eventos.Adicionar(resultado.Valor);
        await eventos.SalvarAlteracoesAsync(cancellationToken);
        return Resultado<int>.Ok(resultado.Valor.Id);
    }

    public async Task<Resultado> AtualizarAsync(int eventoId, DadosEvento dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return Resultado.Falha(EventoNaoEncontrado);
        }

        var comunidade = await comunidades.ObterPorIdAsync(dados.ComunidadeId, cancellationToken);
        if (comunidade is null)
        {
            return Resultado.Falha(ComunidadeInvalida());
        }

        var anterior = $"{evento.Nome}; comunidade {evento.ComunidadeId}; {evento.DataInicio} até {evento.DataFim}";
        var resultado = evento.Atualizar(
            dados.Nome, dados.Descricao, comunidade, dados.DataInicio, dados.DataFim, dados.Observacoes, relogio.GetUtcNow());

        if (resultado.Sucesso) evento.Caixa.Auditar(dados.Operador ?? "Sistema", "Alteração de cadastro", anterior,
            $"{evento.Nome}; comunidade {evento.ComunidadeId}; {evento.DataInicio} até {evento.DataFim}", relogio.GetUtcNow());

        return await SalvarSeSucessoAsync(resultado, cancellationToken);
    }

    public async Task<Resultado> AlterarStatusAsync(int eventoId, AcaoStatusEvento acao, CancellationToken cancellationToken, string? operador = null)
    {
        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return Resultado.Falha(EventoNaoEncontrado);
        }

        var agora = relogio.GetUtcNow();
        var anterior = evento.Status;
        var resultado = acao switch
        {
            AcaoStatusEvento.Ativar => evento.Ativar(agora),
            AcaoStatusEvento.Encerrar => evento.Encerrar(agora),
            AcaoStatusEvento.Cancelar => evento.Cancelar(agora),
            _ => Resultado.Falha(new Erro(string.Empty, "Ação inválida."))
        };

        if (resultado.Sucesso && anterior != evento.Status) evento.Caixa.Auditar(operador ?? "Sistema", "Status da edição",
            anterior.ToString(), evento.Status.ToString(), agora);

        return await SalvarSeSucessoAsync(resultado, cancellationToken);
    }

    public async Task<DadosProduto?> ObterProdutoAsync(int eventoId, int produtoId, CancellationToken cancellationToken)
    {
        var produto = (await eventos.ObterPorIdAsync(eventoId, cancellationToken))?.ObterProduto(produtoId);

        return produto is null ? null : new DadosProduto(produto.Nome, produto.Descricao, produto.Preco, produto.Ativo);
    }

    public async Task<Resultado<int>> AdicionarProdutoAsync(int eventoId, DadosProduto dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return Resultado<int>.Falha(EventoNaoEncontrado);
        }

        var resultado = evento.AdicionarProduto(dados.Nome, dados.Descricao, dados.Preco, relogio.GetUtcNow());
        if (!resultado.Sucesso)
        {
            return Resultado<int>.Falha(resultado.Erros);
        }

        evento.Caixa.Auditar(dados.Operador ?? "Sistema", "Cadastro de produto", "Produto inexistente",
            $"{resultado.Valor.Nome}; preço {resultado.Valor.Preco}", relogio.GetUtcNow());
        await eventos.SalvarAlteracoesAsync(cancellationToken);
        return Resultado<int>.Ok(resultado.Valor.Id);
    }

    public async Task<Resultado> AtualizarProdutoAsync(int eventoId, int produtoId, DadosProduto dados, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var evento = await eventos.ObterPorIdAsync(eventoId, cancellationToken);
        if (evento is null)
        {
            return Resultado.Falha(EventoNaoEncontrado);
        }

        var produto = evento.ObterProduto(produtoId);
        var anterior = produto is null ? "Produto inexistente" : $"{produto.Nome}; preço {produto.Preco}; ativo {produto.Ativo}";
        var resultado = evento.AtualizarProduto(produtoId, dados.Nome, dados.Descricao, dados.Preco, dados.Ativo, relogio.GetUtcNow());
        if (resultado.Sucesso) evento.Caixa.Auditar(dados.Operador ?? "Sistema", "Alteração de produto", anterior,
            $"{dados.Nome}; preço {dados.Preco}; ativo {dados.Ativo}", relogio.GetUtcNow());
        return await SalvarSeSucessoAsync(resultado, cancellationToken);
    }

    private async Task<Resultado> SalvarSeSucessoAsync(Resultado resultado, CancellationToken cancellationToken)
    {
        if (resultado.Sucesso)
        {
            await eventos.SalvarAlteracoesAsync(cancellationToken);
        }

        return resultado;
    }

    private async Task<Dictionary<int, string>> NomesDasComunidadesAsync(CancellationToken cancellationToken) =>
        (await comunidades.ListarAsync(cancellationToken)).ToDictionary(comunidade => comunidade.Id, comunidade => comunidade.NomeCompleto);

    private static Erro ComunidadeInvalida() => new(nameof(DadosEvento.ComunidadeId), "Selecione uma comunidade válida.");

    private static bool ContemSemAcento(string texto, string busca) =>
        CultureInfo.GetCultureInfo("pt-BR").CompareInfo.IndexOf(texto, busca, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
