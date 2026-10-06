using Microsoft.EntityFrameworkCore;
using SaoDimas.Infraestrutura.Persistencia;

namespace SaoDimas.Tests.Infraestrutura;

public sealed class CodigoOriginalModelTests
{
    [Fact]
    public void Migracao_sql_corresponde_ao_modelo_sem_conectar_ao_banco()
    {
        using var contexto = new SaoDimasDbContext(new DbContextOptionsBuilder<SaoDimasDbContext>()
            .UseSqlServer("Server=localhost;Database=TesteModelo;Integrated Security=true;TrustServerCertificate=true")
            .Options);
        Assert.False(contexto.Database.HasPendingModelChanges());
    }
}
