namespace SaoDimas.Aplicacao.Services.Interface;
public interface IDocumentoParoquialService { byte[] Gerar(string titulo,IReadOnlyList<Dictionary<string,string>> registros,string operador); }
