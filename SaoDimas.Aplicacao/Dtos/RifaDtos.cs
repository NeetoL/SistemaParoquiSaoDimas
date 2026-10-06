namespace SaoDimas.Aplicacao.Dtos;

public sealed record RifaDto(Guid Id, string Nome, string Premios, DateOnly DataSorteio, decimal Valor, int Quantidade, int ComunidadeId, string Situacao, int Revisao, List<NumeroRifaDto> Numeros, List<ResultadoRifaDto> Resultados);
public sealed record NumeroRifaDto(int Numero, string Comprador, string Telefone, string Vendedor, bool Pago, string Forma, DateTime ReservadoEm, DateTime? PagoEm);
public sealed record ResultadoRifaDto(int Premio, int Numero, string Comprador, string Referencia, DateTime RegistradoEm);
public sealed record DadosRifa(string Nome, string Premios, DateOnly DataSorteio, decimal Valor, int Quantidade, int ComunidadeId);
