using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class EnvioComunicadoConfiguration : IEntityTypeConfiguration<EnvioComunicado>
{
    public void Configure(EntityTypeBuilder<EnvioComunicado> builder)
    {
        builder.ToTable("envios_comunicado", t => t.HasCheckConstraint(
            "ck_envios_comunicado_status",
            $"""status IN ('{string.Join("','", Enum.GetValues<StatusEnvio>().Select(s => s.ParaValor()))}')"""));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(e => e.ComunicadoExportadoId).IsRequired();
        builder.HasOne<ComunicadoExportado>()
            .WithMany()
            .HasForeignKey(e => e.ComunicadoExportadoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ComunicadoExportadoId);

        builder.Property(e => e.DestinatarioId).IsRequired();
        builder.HasOne<Destinatario>()
            .WithMany()
            .HasForeignKey(e => e.DestinatarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.DestinatarioId);

        builder.Property(e => e.Nome).HasMaxLength(Destinatario.TamanhoMaximoNome).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(Destinatario.TamanhoMaximoEmail).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion(status => status.ParaValor(), valor => StatusEnvioExtensions.DeValor(valor))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Erro).HasMaxLength(EnvioComunicado.TamanhoMaximoErro);
        builder.Property(e => e.EnviadoEm).IsRequired();
    }
}
