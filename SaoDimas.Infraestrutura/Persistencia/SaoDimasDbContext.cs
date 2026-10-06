using Microsoft.EntityFrameworkCore;
using SaoDimas.Dominio.Entities;

namespace SaoDimas.Infraestrutura.Persistencia;

public sealed class SaoDimasDbContext(DbContextOptions<SaoDimasDbContext> options) : DbContext(options)
{
    public DbSet<Comunidade> Comunidades => Set<Comunidade>();

    public DbSet<Dizimista> Dizimistas => Set<Dizimista>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cada entidade tem sua própria classe IEntityTypeConfiguration<T> em Persistencia/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaoDimasDbContext).Assembly);
    }
}
