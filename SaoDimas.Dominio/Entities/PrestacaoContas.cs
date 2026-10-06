namespace SaoDimas.Dominio.Entities;

/// <summary>
/// Prestação de contas de uma faixa de tickets: quantos foram vendidos e devolvidos e quanto dinheiro foi entregue.
/// Os valores esperados e a diferença são sempre calculados aqui (nunca recebidos prontos do navegador).
/// </summary>
public sealed class PrestacaoContas
{
    public const int JustificativaTamanhoMinimo = 5;
    public const int JustificativaTamanhoMaximo = 500;

    private PrestacaoContas(
        int recebidos, int vendidos, int devolvidos, decimal precoUnitario, decimal valorEntregue, string? justificativa, DateTime registradaEm)
    {
        Recebidos = recebidos;
        Vendidos = vendidos;
        Devolvidos = devolvidos;
        PrecoUnitario = precoUnitario;
        ValorEntregue = valorEntregue;
        Justificativa = justificativa;
        RegistradaEm = registradaEm;
    }

    public int Recebidos { get; private set; }

    public int Vendidos { get; private set; }

    public int Devolvidos { get; private set; }

    /// <summary>Preço do lote no momento da prestação.</summary>
    public decimal PrecoUnitario { get; private set; }

    public decimal ValorEntregue { get; private set; }

    public string? Justificativa { get; private set; }

    public DateTime RegistradaEm { get; private set; }

    public decimal ValorEsperado => Calcular(Vendidos, PrecoUnitario, ValorEntregue).ValorEsperado;

    /// <summary>Entregue − esperado: negativo quando falta dinheiro, positivo quando sobra.</summary>
    public decimal Diferenca => Calcular(Vendidos, PrecoUnitario, ValorEntregue).Diferenca;

    public bool PossuiDiferenca => Diferenca != 0;

    /// <summary>
    /// Regra de cálculo da prestação (também usada para pré-visualização, sem registrar).
    /// </summary>
    public static (decimal ValorEsperado, decimal Diferenca) Calcular(int vendidos, decimal precoUnitario, decimal valorEntregue)
    {
        var esperado = vendidos * precoUnitario;
        return (esperado, valorEntregue - esperado);
    }

    internal static Resultado<PrestacaoContas> Criar(
        int recebidos, int vendidos, int devolvidos, decimal precoUnitario, decimal valorEntregue, string? justificativa, DateTimeOffset agora, bool situacaoParcial = false)
    {
        var erros = new List<Erro>();

        if (vendidos < 0)
        {
            erros.Add(new Erro(nameof(Vendidos), "Informe a quantidade vendida (zero ou mais)."));
        }

        if (devolvidos < 0)
        {
            erros.Add(new Erro(nameof(Devolvidos), "Informe a quantidade devolvida (zero ou mais)."));
        }

        if (vendidos >= 0 && devolvidos >= 0 && (situacaoParcial ? (long)vendidos + devolvidos > recebidos : (long)vendidos + devolvidos != recebidos))
        {
            erros.Add(new Erro(nameof(Devolvidos),
                situacaoParcial ? "Vendidos e devolvidos não podem ultrapassar a quantidade recebida." :
                $"Vendidos ({vendidos}) + devolvidos ({devolvidos}) deve ser igual aos {recebidos} tickets recebidos."));
        }

        if (valorEntregue < 0 || !Monetario.EhValido(valorEntregue))
        {
            erros.Add(new Erro(nameof(ValorEntregue), "Informe o valor entregue (zero ou mais, com até duas casas decimais)."));
        }

        var justificativaNormalizada = string.IsNullOrWhiteSpace(justificativa) ? null : justificativa.Trim();
        if (justificativaNormalizada?.Length > JustificativaTamanhoMaximo)
        {
            erros.Add(new Erro(nameof(Justificativa), $"A justificativa deve ter no máximo {JustificativaTamanhoMaximo} caracteres."));
        }

        // Diferença financeira nunca é escondida: exige justificativa.
        if (erros.Count == 0 && Calcular(vendidos, precoUnitario, valorEntregue).Diferenca != 0
            && (justificativaNormalizada?.Length ?? 0) < JustificativaTamanhoMinimo)
        {
            erros.Add(new Erro(nameof(Justificativa), "Há diferença entre o valor esperado e o entregue: informe a justificativa."));
        }

        return erros.Count > 0
            ? Resultado<PrestacaoContas>.Falha(erros)
            : Resultado<PrestacaoContas>.Ok(new PrestacaoContas(
                recebidos, vendidos, devolvidos, precoUnitario, valorEntregue, justificativaNormalizada, agora.UtcDateTime));
    }
}
