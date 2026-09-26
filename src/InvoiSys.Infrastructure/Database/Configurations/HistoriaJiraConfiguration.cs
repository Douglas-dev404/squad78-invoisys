using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento de <see cref="HistoriaJira"/> — sempre filha de uma <see cref="Release"/>
/// (FK "ReleaseId" configurada como shadow property em <see cref="ReleaseConfiguration"/>,
/// já que o domínio não expõe essa referência de volta ao pai).
/// </summary>
public sealed class HistoriaJiraConfiguration : IEntityTypeConfiguration<HistoriaJira>
{
    public void Configure(EntityTypeBuilder<HistoriaJira> builder)
    {
        builder.ToTable("historias_jira");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(h => h.Chave).HasMaxLength(50).IsRequired();
        builder.Property(h => h.Titulo).HasColumnType("text").IsRequired();
        builder.Property(h => h.DescricaoTecnica).HasColumnType("text").IsRequired();
        builder.Property(h => h.TipoIssue).HasMaxLength(50).IsRequired();
        builder.Property(h => h.TextoReleaseNote).HasColumnType("text");

        builder.Property(h => h.Labels)
            .HasColumnType("text[]")
            .HasConversion(
                lista => lista.ToArray(),
                array => (IReadOnlyList<string>)array.ToList())
            .Metadata.SetValueComparer(ArrayConversionHelper.ListaStringComparer);

        builder.Property<DateTimeOffset>("CriadoEm").HasDefaultValueSql("now()");

        // Unicidade de negócio: a mesma issue do Jira não se repete dentro da mesma Release.
        builder.HasIndex("ReleaseId", nameof(HistoriaJira.Chave)).IsUnique();

        // Calculadas a partir de outras colunas — não persistem.
        builder.Ignore(h => h.PossuiReleaseNoteDedicada);
        builder.Ignore(h => h.TextoFonte);
    }
}
