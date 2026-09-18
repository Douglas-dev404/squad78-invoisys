using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class DestaqueHeroConfiguration : IEntityTypeConfiguration<DestaqueHero>
{
    public void Configure(EntityTypeBuilder<DestaqueHero> builder)
    {
        builder.ToTable("destaques_hero");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Descricao).HasColumnType("text").IsRequired();
        builder.Property(d => d.Icone).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Ordem).IsRequired().HasDefaultValue(0);
        builder.Property(d => d.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.CriadoEm).HasDefaultValueSql("now()");

        builder.HasIndex(d => new { d.Ativo, d.Ordem });
    }
}
