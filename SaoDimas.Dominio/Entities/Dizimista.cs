using SaoDimas.Dominio.Enums;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Fiel que contribui com o dízimo, vinculado a uma <see cref="Comunidade"/> de referência.
/// A comunidade é referenciada pelo identificador (agregados distintos).
/// </summary>
public sealed class Dizimista
{
    public const int NomeTamanhoMaximo = 150;

    private static readonly DateOnly DataEntradaMinima = new(1900, 1, 1);

    private Dizimista(string nome, DateTime criadoEm)
    {
        Nome = nome;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public string? CodigoOriginal { get; private set; }

    public string Codigo => CodigoOriginal ?? FormatarCodigo(Id);

    public string Nome { get; private set; }

    public Cpf? Cpf { get; private set; }

    public Telefone? Telefone { get; private set; }

    public string? Endereco { get; private set; }
    public string? Cep { get; private set; }
    public string? Bairro { get; private set; }
    public DateOnly? DataNascimento { get; private set; }

    public int ComunidadeId { get; private set; }

    public DateOnly DataEntrada { get; private set; }

    public StatusDizimista Status { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? AtualizadoEm { get; private set; }

    public static string FormatarCodigo(int id) => id.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    public static Resultado<Dizimista> Criar(
        string nome,
        string? cpf,
        string? telefone,
        Comunidade comunidade,
        DateOnly dataEntrada,
        StatusDizimista status,
        DateTimeOffset agora, string? endereco = null, string? cep = null, string? bairro = null, DateOnly? dataNascimento = null, string? codigoOriginal = null)
    {
        ArgumentNullException.ThrowIfNull(comunidade);

        var dizimista = new Dizimista(string.Empty, agora.UtcDateTime);
        var resultado = dizimista.Aplicar(nome, cpf, telefone, comunidade, novoVinculo: true, dataEntrada, status, agora, endereco, cep, bairro, dataNascimento, codigoOriginal);

        return resultado.Sucesso
            ? Resultado<Dizimista>.Ok(dizimista)
            : Resultado<Dizimista>.Falha(resultado.Erros);
    }

    public Resultado Atualizar(
        string nome,
        string? cpf,
        string? telefone,
        Comunidade comunidade,
        DateOnly dataEntrada,
        StatusDizimista status,
        DateTimeOffset agora, string? endereco = null, string? cep = null, string? bairro = null, DateOnly? dataNascimento = null, string? codigoOriginal = null)
    {
        ArgumentNullException.ThrowIfNull(comunidade);

        var resultado = Aplicar(nome, cpf, telefone, comunidade, novoVinculo: comunidade.Id != ComunidadeId, dataEntrada, status, agora, endereco, cep, bairro, dataNascimento, codigoOriginal);

        if (resultado.Sucesso)
        {
            AtualizadoEm = agora.UtcDateTime;
        }

        return resultado;
    }

    /// <summary>
    /// Valida todos os dados e só altera o estado se não houver erros.
    /// </summary>
    private Resultado Aplicar(
        string nome,
        string? cpf,
        string? telefone,
        Comunidade comunidade,
        bool novoVinculo,
        DateOnly dataEntrada,
        StatusDizimista status,
        DateTimeOffset agora, string? endereco = null, string? cep = null, string? bairro = null, DateOnly? dataNascimento = null, string? codigoOriginal = null)
    {
        var erros = new List<Erro>();
        var codigoNormalizado = codigoOriginal?.Trim();
        if (codigoOriginal is not null && (string.IsNullOrWhiteSpace(codigoNormalizado) || codigoNormalizado.Length > 50 || codigoNormalizado.Any(char.IsWhiteSpace) || codigoNormalizado.Any(char.IsControl)))
            erros.Add(new Erro(nameof(CodigoOriginal), "Informe um código de até 50 caracteres, sem espaços."));

        var nomeNormalizado = Texto.NormalizarEspacos(nome);
        if (nomeNormalizado.Length == 0)
        {
            erros.Add(new Erro(nameof(Nome), "Informe o nome."));
        }
        else if (nomeNormalizado.Length > NomeTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Nome), $"O nome deve ter no máximo {NomeTamanhoMaximo} caracteres."));
        }

        Cpf? novoCpf = null;
        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var resultadoCpf = Cpf.Criar(cpf, nameof(Cpf));
            if (resultadoCpf.Sucesso) novoCpf = resultadoCpf.Valor;
            else erros.AddRange(resultadoCpf.Erros);
        }

        Telefone? novoTelefone = null;
        if (!string.IsNullOrWhiteSpace(telefone))
        {
            var resultadoTelefone = Telefone.Criar(telefone, nameof(Telefone));
            if (resultadoTelefone.Sucesso) novoTelefone = resultadoTelefone.Valor;
            else erros.AddRange(resultadoTelefone.Erros);
        }

        // Um novo vínculo (cadastro ou troca) exige comunidade apta; manter o vínculo atual é sempre permitido.
        if (novoVinculo && !comunidade.PodeReceberVinculos)
        {
            erros.Add(new Erro(nameof(ComunidadeId), "A comunidade selecionada está inativa e não pode receber novos dizimistas."));
        }

        var hoje = DateOnly.FromDateTime(agora.DateTime);
        if (dataEntrada > hoje)
        {
            erros.Add(new Erro(nameof(DataEntrada), "A data de entrada não pode ser futura."));
        }
        else if (dataEntrada < DataEntradaMinima)
        {
            erros.Add(new Erro(nameof(DataEntrada), "Data de entrada inválida."));
        }

        if (!Enum.IsDefined(status))
        {
            erros.Add(new Erro(nameof(Status), "Status inválido."));
        }

        var enderecoNormalizado = Texto.NormalizarEspacos(endereco ?? string.Empty);
        var bairroNormalizado = Texto.NormalizarEspacos(bairro ?? string.Empty);
        var cepNormalizado = Texto.SomenteDigitos(cep);
        if (enderecoNormalizado.Length > 250) erros.Add(new Erro(nameof(Endereco), "O endereço deve ter no máximo 250 caracteres."));
        if (bairroNormalizado.Length > 100) erros.Add(new Erro(nameof(Bairro), "O bairro deve ter no máximo 100 caracteres."));
        if (!string.IsNullOrWhiteSpace(cep) && (cepNormalizado.Length != 8 || cep!.Any(c => !char.IsDigit(c) && c != '-' && !char.IsWhiteSpace(c))))
            erros.Add(new Erro(nameof(Cep), "Informe um CEP com 8 dígitos."));
        if (dataNascimento is not null && (dataNascimento > hoje || dataNascimento < DataEntradaMinima))
            erros.Add(new Erro(nameof(DataNascimento), "Informe uma data de nascimento válida, que não seja futura."));

        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Endereco = enderecoNormalizado.Length == 0 ? null : enderecoNormalizado;
        Bairro = bairroNormalizado.Length == 0 ? null : bairroNormalizado;
        Cep = cepNormalizado.Length == 0 ? null : cepNormalizado;
        DataNascimento = dataNascimento;
        if (codigoNormalizado is not null) CodigoOriginal = codigoNormalizado;
        Nome = nomeNormalizado;
        Cpf = novoCpf;
        Telefone = novoTelefone;
        ComunidadeId = comunidade.Id;
        DataEntrada = dataEntrada;
        Status = status;

        return Resultado.Ok();
    }
}
