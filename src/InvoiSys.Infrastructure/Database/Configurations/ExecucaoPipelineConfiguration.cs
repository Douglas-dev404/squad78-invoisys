using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class ExecucaoPipelineConfiguration : IEntityTypeConfiguration<ExecucaoPipeline>
{
    public void Configure(EntityTypeBuilder<ExecucaoPipeline> builder)
    {
        builder.ToTable("execucoes_pipeline", t => t.HasCheckConstraint(
            "ck_execucoes_pipeline_status",
            $"""status IN ('{string.Join("','", Enum.GetValues<StatusExecucaoPipeline>().Select(s => s.ParaValor()))}')"""));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(e => e.ReleaseId).IsRequired();
        builder.HasIndex(e => e.ReleaseId);

        builder.Property(e => e.Status)
            .HasConversion(
                status => status.ParaValor(),
                valor => ParseStatus(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.ModeloLlm).HasMaxLength(100);
        builder.Property(e => e.Erro).HasColumnType("text");
        builder.Property(e => e.IniciadoEm).IsRequired();
        builder.Property(e => e.ConcluidoEm);
    }

    private static StatusExecucaoPipeline ParseStatus(string valor) =>
        Enum.GetValues<StatusExecucaoPipeline>().First(s => s.ParaValor() == valor);
}
