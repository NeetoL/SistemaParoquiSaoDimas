using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Utilitarios;

public static class ValidacaoRifas
{
    public static bool Valida(RifaDto? r)
    {
        if (r is null || r.Id == Guid.Empty || r.Revisao < 1 || string.IsNullOrWhiteSpace(r.Nome) || r.Nome.Length > 150 || string.IsNullOrWhiteSpace(r.Premios) || r.Premios.Length > 2000 || r.Quantidade is < 1 or > 10000 || r.Valor <= 0 || r.Valor > 100000 || decimal.Round(r.Valor, 2) != r.Valor || r.ComunidadeId < 1 || r.Situacao is not ("Aberta" or "Fechada" or "Concluída" or "Cancelada") || r.Numeros is null || r.Resultados is null) return false;
        var premios = r.Premios.Split('\n'); if (premios.Length is < 1 or > 20 || premios.Any(string.IsNullOrWhiteSpace)) return false;
        if (r.Numeros.Any(n => n is null || n.Numero < 1 || n.Numero > r.Quantidade || string.IsNullOrWhiteSpace(n.Comprador) || n.Comprador.Length > 150 || n.Telefone is null || n.Telefone.Length > 40 || n.Vendedor is null || n.Vendedor.Length > 150 || n.Forma is null || n.Pago && (n.PagoEm is null || n.Forma is not ("Dinheiro" or "PIX" or "Transferência" or "Cartão" or "Outro"))) || r.Numeros.Select(n => n.Numero).Distinct().Count() != r.Numeros.Count) return false;
        if (r.Resultados.Any(x => x is null || x.Premio < 1 || x.Premio > premios.Length || string.IsNullOrWhiteSpace(x.Referencia) || x.Referencia.Length > 500 || !r.Numeros.Any(n => n.Numero == x.Numero && n.Pago && n.Comprador == x.Comprador)) || r.Resultados.Select(x => x.Premio).Distinct().Count() != r.Resultados.Count) return false;
        return !(r.Situacao == "Concluída" && r.Resultados.Count != premios.Length || r.Situacao == "Cancelada" && r.Numeros.Any(n => n.Pago) || r.Resultados.Count > 0 && r.Situacao is "Aberta" or "Cancelada");
    }
}
