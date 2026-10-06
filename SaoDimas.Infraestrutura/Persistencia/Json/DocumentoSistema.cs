using SaoDimas.Dominio.Enums;
using SaoDimas.Aplicacao.Dtos;
using SaoDimas.Infraestrutura.Persistencia.Temporaria;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal sealed class DocumentoSistema
{
    public List<RegistroParoquial> Gestao { get; set; } = [];
    public List<UsuarioParoquial> Usuarios { get; set; } = [];
    public List<AuditoriaParoquial> Auditoria { get; set; } = [];
    public int Versao { get; set; } = 1;
    public int SequenciaDizimista { get; set; }
    public List<ComunidadeRegistro> Comunidades { get; set; } = [];
    public List<DizimistaRegistro> Dizimistas { get; set; } = [];
    public DocumentoEventos Eventos { get; set; } = new();
    public bool EventosLegadosImportados { get; set; }
}
internal sealed record ComunidadeRegistro(int Id, string Nome, TipoComunidade Tipo, bool Ativa, int OrdemExibicao, DateTime CriadoEm, DateTime? AtualizadoEm);
internal sealed record DizimistaRegistro(int Id, string Nome, string? Cpf, string? Telefone, int ComunidadeId,
    DateOnly DataEntrada, StatusDizimista Status, DateTime CriadoEm, DateTime? AtualizadoEm,
    string? Endereco, string? Cep, string? Bairro, DateOnly? DataNascimento);
