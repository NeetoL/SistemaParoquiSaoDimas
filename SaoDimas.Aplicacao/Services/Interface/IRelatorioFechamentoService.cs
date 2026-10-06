using SaoDimas.Aplicacao.Dtos;

namespace SaoDimas.Aplicacao.Services.Interface;

public interface IRelatorioFechamentoService
{
    byte[] Gerar(GestaoEventoDto dados);
}
