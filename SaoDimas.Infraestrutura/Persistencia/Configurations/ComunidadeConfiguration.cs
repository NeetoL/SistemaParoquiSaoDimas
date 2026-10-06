using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaoDimas.Dominio.Entities;
using SaoDimas.Dominio.Enums;

namespace SaoDimas.Infraestrutura.Persistencia.Configurations;

internal sealed class ComunidadeConfiguration : IEntityTypeConfiguration<Comunidade>
{
    public void Configure(EntityTypeBuilder<Comunidade> builder)
    {
        builder.ToTable("Comunidades", tabela =>
        {
            tabela.HasCheckConstraint("CK_Comunidades_Tipo", "[Tipo] IN (1, 2)");
            tabela.HasCheckConstraint("CK_Comunidades_OrdemExibicao", "[OrdemExibicao] >= 0");
        });

        builder.HasKey(comunidade => comunidade.Id);

        builder.Property(comunidade => comunidade.Nome)
            .HasMaxLength(Comunidade.NomeTamanhoMaximo)
            .IsRequired();

        builder.Property(comunidade => comunidade.Tipo).IsRequired();
        builder.Property(comunidade => comunidade.Ativa).IsRequired();
        builder.Property(comunidade => comunidade.OrdemExibicao).IsRequired();
        builder.Property(comunidade => comunidade.CriadoEm).IsRequired();

        builder.Ignore(comunidade => comunidade.NomeCompleto);
        builder.Ignore(comunidade => comunidade.PodeReceberVinculos);

        builder.HasIndex(comunidade => comunidade.Nome).IsUnique();
        builder.HasIndex(comunidade => comunidade.OrdemExibicao);

        // A paróquia possui uma única Matriz.
        builder.HasIndex(comunidade => comunidade.Tipo)
            .IsUnique()
            .HasFilter($"[Tipo] = {(int)TipoComunidade.Matriz}")
            .HasDatabaseName("UX_Comunidades_MatrizUnica");

        // Comunidades iniciais da Paróquia São Dimas. Novas capelas e alterações virão da
        // administração de Comunidades; estes nomes não devem ser usados em nenhum outro ponto do código.
        var criadoEm = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new { Id = 1, Nome = "Paróquia São Dimas", Tipo = TipoComunidade.Matriz, Ativa = true, OrdemExibicao = 1, CriadoEm = criadoEm },
            new { Id = 2, Nome = "Santa Teresinha", Tipo = TipoComunidade.Capela, Ativa = true, OrdemExibicao = 2, CriadoEm = criadoEm },
            new { Id = 3, Nome = "Santo Inácio", Tipo = TipoComunidade.Capela, Ativa = true, OrdemExibicao = 3, CriadoEm = criadoEm },
            new { Id = 4, Nome = "Santo Expedito", Tipo = TipoComunidade.Capela, Ativa = true, OrdemExibicao = 4, CriadoEm = criadoEm });
    }
}
