using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class DestinatarioConfiguration : IEntityTypeConfiguration<Destinatario>
{
    public void Configure(EntityTypeBuilder<Destinatario> builder)
    {
        builder.ToTable("destinatarios", t => t.HasCheckConstraint(
            "ck_destinatarios_publico",
            $"""publico IN ('{string.Join("','", Enum.GetValues<PublicoAlvo>().Select(p => p.ParaValor()))}')"""));

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(d => d.Nome).HasMaxLength(Destinatario.TamanhoMaximoNome).IsRequired();
        builder.Property(d => d.Email).HasMaxLength(Destinatario.TamanhoMaximoEmail).IsRequired();

        builder.Property(d => d.Publico)
            .HasConversion(publico => publico.ParaValor(), valor => ParsePublico(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(d => new { d.Email, d.Publico }).IsUnique();

        builder.Property(d => d.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.CriadoEm).HasDefaultValueSql("now()");
    }

    private static PublicoAlvo ParsePublico(string valor) =>
        Enum.GetValues<PublicoAlvo>().First(p => p.ParaValor() == valor);
}
