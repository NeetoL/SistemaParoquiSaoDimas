namespace SaoDimas.Aplicacao.Services.Interface;
/// <summary>Carrega os eventos do servidor e salva a unidade de trabalho da requisição.</summary>
public interface IEstadoEventosServidor
{
    Task<bool> CarregarAsync(string? documentoLegado, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
