using System.Diagnostics;
using SaoDimas.Dominio.Entities;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal sealed class SessaoSistemaJson(ArquivoSistemaJson arquivo) : IDisposable
{
    private FileStream? _bloqueio;
    private bool _carregada;
    private bool _possuiExclusao;
    public DocumentoSistema Documento { get; private set; } = null!;
    public List<Comunidade> Comunidades { get; private set; } = [];
    public List<Dizimista> Dizimistas { get; private set; } = [];
    public async Task CarregarAsync(CancellationToken ct)
    {
        if (_carregada) return;
        await arquivo.Exclusao.WaitAsync(ct);
        _possuiExclusao = true;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo.Caminho)!);
            var espera = Stopwatch.StartNew();
            while (_bloqueio is null)
            {
                ct.ThrowIfCancellationRequested();
                try { _bloqueio = new FileStream(arquivo.Caminho + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) when (espera.Elapsed < TimeSpan.FromSeconds(30)) { await Task.Delay(50, ct); }
            }
            Documento = await arquivo.LerAsync(ct);
            Comunidades = Documento.Comunidades.Select(MapeadorCadastrosJson.ParaEntidade).ToList();
            Dizimistas = Documento.Dizimistas.Select(MapeadorCadastrosJson.ParaEntidade).ToList();
            _carregada = true;
            if (!File.Exists(arquivo.Caminho)) await SalvarAsync(ct);
        }
        catch { Dispose(); throw; }
    }
    public async Task SalvarAsync(CancellationToken ct)
    {
        if (!_carregada) throw new InvalidOperationException("Carregue os dados antes de salvar.");
        foreach (var dizimista in Dizimistas.Where(d => d.Id == 0))
        {
            Documento.SequenciaDizimista = Math.Max(Documento.SequenciaDizimista, Dizimistas.Max(d => d.Id));
            MapeadorCadastrosJson.Definir(dizimista, nameof(Dizimista.Id), ++Documento.SequenciaDizimista);
        }
        Documento.Comunidades = Comunidades.Select(MapeadorCadastrosJson.ParaRegistro).ToList();
        Documento.Dizimistas = Dizimistas.Select(MapeadorCadastrosJson.ParaRegistro).ToList();
        await arquivo.GravarAsync(Documento, ct);
    }
    public void Dispose()
    {
        _bloqueio?.Dispose(); _bloqueio = null;
        if (_possuiExclusao) { _possuiExclusao = false; arquivo.Exclusao.Release(); }
    }
}
