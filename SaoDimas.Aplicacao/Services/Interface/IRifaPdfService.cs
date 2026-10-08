using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.Aplicacao.Services.Interface;

public interface IRifaPdfService
{
    byte[] Gerar(RifaDto rifa, string comunidade, int inicio, int fim);
}
