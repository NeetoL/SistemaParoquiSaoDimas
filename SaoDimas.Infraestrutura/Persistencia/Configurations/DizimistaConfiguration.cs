using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.ValueObjects;

namespace SaoDimas.Infraestrutura.Persistencia.Configurations;

internal sealed class DizimistaConfiguration : IEntityTypeConfiguration<Dizimista>
{
    /// <summary>
    /// Pesquisa por nome sem diferenciar maiúsculas nem acentos ("joao" encontra "João").
    /// </summary>
    private const string CollationPesquisa = "Latin1_General_100_CI_AI";

    public void Configure(EntityTypeBuilder<Dizimista> builder)
    {
        builder.ToTable("Dizimistas", tabela =>
            tabela.HasCheckConstraint("CK_Dizimistas_Status", "[Status] IN (1, 2)"));

        builder.HasKey(dizimista => dizimista.Id);

        builder.Property(dizimista => dizimista.Nome)
            .HasMaxLength(Dizimista.NomeTamanhoMaximo)
            .UseCollation(CollationPesquisa)
            .IsRequired();

        builder.Property(dizimista => dizimista.Cpf)
            .HasConversion(cpf => cpf!.Valor, valor => Cpf.Reconstituir(valor))
            .HasMaxLength(Cpf.Tamanho)
            .IsFixedLength()
            .IsUnicode(false);

        builder.Property(dizimista => dizimista.Telefone)
            .HasConversion(telefone => telefone!.Valor, valor => Telefone.Reconstituir(valor))
            .HasMaxLength(Telefone.TamanhoMaximo)
            .IsUnicode(false);

        builder.Property(d => d.Endereco).HasMaxLength(250);
        builder.Property(d => d.Bairro).HasMaxLength(100);
        builder.Property(d => d.Cep).HasMaxLength(8).IsUnicode(false);
        builder.Property(d => d.DataNascimento);

        builder.Property(dizimista => dizimista.DataEntrada).IsRequired();
        builder.Property(dizimista => dizimista.Status).IsRequired();
        builder.Property(dizimista => dizimista.CriadoEm).IsRequired();

        builder.Ignore(dizimista => dizimista.Codigo);

        // Comunidade 1:N Dizimista. Uma comunidade com dizimistas não pode ser excluída (deve ser inativada).
        builder.HasOne<Comunidade>()
            .WithMany()
            .HasForeignKey(dizimista => dizimista.ComunidadeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(dizimista => dizimista.Cpf)
            .IsUnique()
            .HasFilter("[Cpf] IS NOT NULL");

        builder.HasIndex(dizimista => dizimista.Nome);
        builder.HasIndex(dizimista => new { dizimista.ComunidadeId, dizimista.Nome });
    }
}
