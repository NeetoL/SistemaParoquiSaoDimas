using System.Runtime.CompilerServices;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.ValueObjects;
namespace SaoDimas.Infraestrutura.Persistencia.Json;
internal static class MapeadorCadastrosJson
{
    public static Comunidade ParaEntidade(ComunidadeRegistro registro) => Reconstituir<Comunidade>(registro);
    public static Dizimista ParaEntidade(DizimistaRegistro registro)
    {
        var entidade = Reconstituir<Dizimista>(registro);
        Definir(entidade, nameof(Dizimista.Cpf), registro.Cpf is null ? null : Cpf.Reconstituir(registro.Cpf));
        Definir(entidade, nameof(Dizimista.Telefone), registro.Telefone is null ? null : Telefone.Reconstituir(registro.Telefone));
        return entidade;
    }
    private static T Reconstituir<T>(object registro) where T : class
    {
        var entidade = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        foreach (var propriedade in registro.GetType().GetProperties())
        {
            if (typeof(T) == typeof(Dizimista) && propriedade.Name is nameof(Dizimista.Cpf) or nameof(Dizimista.Telefone)) continue;
            Definir(entidade, propriedade.Name, propriedade.GetValue(registro));
        }
        return entidade;
    }
    public static void Definir(object entidade, string propriedade, object? valor) =>
        entidade.GetType().GetProperty(propriedade)!.SetValue(entidade, valor);
    public static ComunidadeRegistro ParaRegistro(Comunidade c) => new(c.Id, c.Nome, c.Tipo, c.Ativa, c.OrdemExibicao, c.CriadoEm, c.AtualizadoEm);
    public static DizimistaRegistro ParaRegistro(Dizimista d) => new(d.Id, d.Nome, d.Cpf?.Valor, d.Telefone?.Valor, d.ComunidadeId,
        d.DataEntrada, d.Status, d.CriadoEm, d.AtualizadoEm, d.Endereco, d.Cep, d.Bairro, d.DataNascimento);
}
