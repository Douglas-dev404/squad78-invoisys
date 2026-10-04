using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

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

        builder.HasIndex("ReleaseId", nameof(HistoriaJira.Chave)).IsUnique();

        builder.Ignore(h => h.PossuiReleaseNoteDedicada);
        builder.Ignore(h => h.TextoFonte);
    }
}
