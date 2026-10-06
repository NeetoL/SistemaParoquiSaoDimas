namespace SaoDimas.Tests.Arquitetura;

/// <summary>
/// Garante que as dependências entre camadas apontem sempre para o núcleo.
/// </summary>
public sealed class DependenciasEntreCamadasTests
{
    [Theory]
    [InlineData("SaoDimas.Dominio", "SaoDimas.Aplicacao")]
    [InlineData("SaoDimas.Dominio", "SaoDimas.Infraestrutura")]
    [InlineData("SaoDimas.Dominio", "SaoDimas.MVC")]
    [InlineData("SaoDimas.Dominio", "Microsoft.EntityFrameworkCore")]
    [InlineData("SaoDimas.Dominio", "Microsoft.AspNetCore")]
    [InlineData("SaoDimas.Dominio", "Microsoft.Data.SqlClient")]
    [InlineData("SaoDimas.Dominio", "Microsoft.Extensions.DependencyInjection")]
    [InlineData("SaoDimas.Aplicacao", "SaoDimas.Infraestrutura")]
    [InlineData("SaoDimas.Aplicacao", "SaoDimas.MVC")]
    [InlineData("SaoDimas.Aplicacao", "Microsoft.EntityFrameworkCore")]
    [InlineData("SaoDimas.Aplicacao", "Microsoft.AspNetCore")]
    [InlineData("SaoDimas.Infraestrutura", "SaoDimas.MVC")]
    [InlineData("SaoDimas.Infraestrutura", "Microsoft.AspNetCore")]
    [InlineData("SaoDimas.Dominio", "SaoDimas.CrossCutting")]
    [InlineData("SaoDimas.Aplicacao", "SaoDimas.CrossCutting")]
    [InlineData("SaoDimas.Infraestrutura", "SaoDimas.CrossCutting")]
    [InlineData("SaoDimas.CrossCutting", "SaoDimas.MVC")]
    [InlineData("SaoDimas.CrossCutting", "SaoDimas.Infraestrutura")]
    [InlineData("SaoDimas.MVC", "Microsoft.EntityFrameworkCore")]
    public void Camada_nao_deve_depender_de(string camada, string dependenciaProibida)
    {
        var referencias = Camadas.Todas
            .Single(assembly => assembly.GetName().Name == camada)
            .GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty);

        Assert.DoesNotContain(referencias, nome => nome.StartsWith(dependenciaProibida, StringComparison.Ordinal));
    }
}
