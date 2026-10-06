namespace SaoDimas.Aplicacao.Services.Interface;

/// <summary>
/// Persistência TEMPORÁRIA do módulo de Eventos: os dados ficam no navegador (localStorage) e viajam como um
/// documento JSON em cada requisição. A apresentação carrega o documento antes do caso de uso e devolve ao
/// navegador a versão alterada. Os repositórios de Eventos usam este estado por baixo; ao migrar para
/// SQL Server/EF Core, basta trocar os repositórios e deixar de usar este contrato.
/// </summary>
public interface IEstadoEventosNavegador
{
    /// <returns>Falso se o documento recebido for inválido (nada é carregado).</returns>
    bool Carregar(string? documentoJson);

    /// <summary>Documento atualizado, se algum caso de uso salvou alterações nesta requisição.</summary>
    string? ObterDocumentoAlterado();
}
