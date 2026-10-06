using System.Text.Json;
using System.Globalization;
using SaoDimas.Aplicacao.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SaoDimas.Dominio.Enums;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal sealed class ArquivoSistemaJson : IDisposable
{
    internal static readonly JsonSerializerOptions Serializacao = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    internal SemaphoreSlim Exclusao { get; } = new(1, 1);
    public string Caminho { get; }
    private readonly Lazy<Dictionary<(int Comunidade, string Nome), List<DizimistaRegistro>>> _codigosOriginais;
    public ArquivoSistemaJson(IHostEnvironment ambiente, IConfiguration configuracao)
    {
        var diretorio = configuracao["Persistencia:Diretorio"] ?? "App_Data";
        Caminho = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, diretorio, "sistema.json"));
        var publico = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, "wwwroot")) + Path.DirectorySeparatorChar;
        if (Caminho.StartsWith(publico, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Os dados JSON devem ficar fora de wwwroot.");
        var origem = configuracao["Persistencia:CadastrosIniciais"]
            ?? Path.Combine(ambiente.ContentRootPath, "..", "dados-iniciais", "cadastros.json");
        _codigosOriginais = new(() =>
        {
            if (!File.Exists(origem)) return [];
            var iniciais = JsonSerializer.Deserialize<DocumentoSistema>(File.ReadAllText(origem), Serializacao)
                ?? throw new InvalidDataException("O arquivo de cadastros iniciais é inválido.");
            return iniciais.Dizimistas.Where(d => !string.IsNullOrWhiteSpace(d.CodigoOriginal))
                .GroupBy(d => (d.ComunidadeId, d.Nome.ToUpperInvariant()))
                .ToDictionary(g => g.Key, g => g.ToList());
        });
    }
    internal async Task<DocumentoSistema> LerAsync(CancellationToken ct)
    {
        if (!File.Exists(Caminho)) return Inicial();
        await using var arquivo = File.OpenRead(Caminho);
        var documento = await JsonSerializer.DeserializeAsync<DocumentoSistema>(arquivo, Serializacao, ct);
        Validar(documento);
        for (var i = 0; i < documento!.Dizimistas.Count; i++)
        {
            var pessoa = documento.Dizimistas[i];
            if (!string.IsNullOrWhiteSpace(pessoa.CodigoOriginal)
                || !_codigosOriginais.Value.TryGetValue((pessoa.ComunidadeId, pessoa.Nome.ToUpperInvariant()), out var fontes)) continue;
            var correspondentes = fontes.Where(f => f.Id == pessoa.Id
                || (f.Telefone == pessoa.Telefone && f.Endereco == pessoa.Endereco)).ToList();
            if (correspondentes.Count == 1)
                documento.Dizimistas[i] = pessoa with { CodigoOriginal = correspondentes[0].CodigoOriginal };
        }
        return documento!;
    }
    internal static void Validar(DocumentoSistema? documento)
    {
        if (documento is null || documento.Versao != 1 || documento.Comunidades is null || documento.Dizimistas is null || documento.Eventos is null || documento.Gestao is null || documento.Usuarios is null || documento.Auditoria is null
            || documento.Comunidades.Any(c => c is null) || documento.Dizimistas.Any(d => d is null)
            || documento.Eventos.Eventos is null || documento.Eventos.Lotes is null || documento.Eventos.Sequencias is null || documento.Eventos.EventosBase is null
            || documento.Eventos.Versao is not (1 or 2)
            || documento.Eventos.Eventos.Any(e => e is null || e.Produtos is null)
            || documento.Eventos.Lotes.Any(l => l is null || l.Distribuicoes is null)
            || documento.Comunidades.Select(c => c.Id).Distinct().Count() != documento.Comunidades.Count
            || documento.Dizimistas.Select(d => d.Id).Distinct().Count() != documento.Dizimistas.Count
            || documento.Dizimistas.Any(d => !documento.Comunidades.Any(c => c.Id == d.ComunidadeId)))
            throw new InvalidDataException("O arquivo JSON de dados é inválido. Restaure a cópia de segurança; o arquivo não foi sobrescrito.");
        ValidarGestao(documento);
    }
    private static void ValidarGestao(DocumentoSistema documento)
    {
        if (documento.Gestao.Any(r => r is null || r.Id == Guid.Empty || r.Revisao < 1 || r.Campos is null ||
                CatalogoParoquial.Obter(r.Modulo) is null) ||
            documento.Gestao.Select(r => r.Id).Distinct().Count() != documento.Gestao.Count ||
            documento.Usuarios.Any(u => u is null || u.Id == Guid.Empty || string.IsNullOrWhiteSpace(u.Login) ||
                string.IsNullOrWhiteSpace(u.SenhaHash) || !CatalogoParoquial.Perfis.Contains(u.Perfil)) ||
            documento.Usuarios.Select(u => u.Id).Distinct().Count() != documento.Usuarios.Count ||
            documento.Usuarios.Select(u => u.Login.ToUpperInvariant()).Distinct().Count() != documento.Usuarios.Count)
            throw new InvalidDataException("O backup contém registros ou usuários inválidos.");
        foreach (var registro in documento.Gestao)
        {
            var catalogo = CatalogoParoquial.Obter(registro.Modulo)!;
            if (registro.Campos.Keys.Any(k => !catalogo.Campos.Any(c => c.Chave == k)))
                throw new InvalidDataException("O backup contém campos desconhecidos.");
            foreach (var campo in catalogo.Campos)
            {
                var valor = registro.Campos.GetValueOrDefault(campo.Chave, "");
                if (valor is null || (campo.Obrigatorio && string.IsNullOrWhiteSpace(valor)) || valor.Length > 2000 ||
                    (valor.Length > 0 && campo.Opcoes is not null && !campo.Opcoes.Contains(valor)) ||
                    (campo.Tipo == "money" && (!decimal.TryParse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var quantia) || quantia <= 0 || quantia > 100000000 || decimal.Round(quantia, 2) != quantia)) ||
                    (valor.Length > 0 && campo.Tipo == "date" && !DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) ||
                    (valor.Length > 0 && campo.Tipo == "datetime-local" && !DateTime.TryParseExact(valor, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) ||
                    (valor.Length > 0 && campo.Tipo == "month" && !DateTime.TryParseExact(valor, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                    throw new InvalidDataException("O backup contém valores inválidos em " + campo.Nome + ".");
            }
            if (registro.AnexoBase64 is not null)
            {
                try { if (Convert.FromBase64String(registro.AnexoBase64).Length > 5 * 1024 * 1024) throw new InvalidDataException("Comprovante excede 5 MB."); }
                catch (FormatException e) { throw new InvalidDataException("Comprovante inválido no backup.", e); }
            }
        }
    }
    internal async Task GravarAsync(DocumentoSistema documento, CancellationToken ct)
    {
        var temporario = Caminho + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var arquivo = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(arquivo, documento, Serializacao, ct);
                await arquivo.FlushAsync(ct);
                arquivo.Flush(flushToDisk: true);
            }
            ct.ThrowIfCancellationRequested();
            if (File.Exists(Caminho)) {
                var pasta=Path.Combine(Path.GetDirectoryName(Caminho)!,"backups");Directory.CreateDirectory(pasta);
                var diario=Path.Combine(pasta,"automatico-"+DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)+".json");
                if(!File.Exists(diario)) File.Copy(Caminho,diario);
                foreach(var antigo in Directory.GetFiles(pasta,"automatico-*.json").OrderDescending().Skip(30))File.Delete(antigo);
                File.Copy(Caminho,Caminho+".bak",overwrite:true);
            }
            File.Move(temporario, Caminho, overwrite: true);
        }
        finally { if (File.Exists(temporario)) File.Delete(temporario); }
    }
    private static DocumentoSistema Inicial()
    {
        var criado = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new DocumentoSistema { Comunidades = [
            new(1, "Paróquia São Dimas", TipoComunidade.Matriz, true, 1, criado, null),
            new(2, "Santa Teresinha", TipoComunidade.Capela, true, 2, criado, null),
            new(3, "Santo Inácio", TipoComunidade.Capela, true, 3, criado, null),
            new(4, "Santo Expedito", TipoComunidade.Capela, true, 4, criado, null)] };
    }
    public void Dispose() => Exclusao.Dispose();
}
