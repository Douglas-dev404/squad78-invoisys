using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento de <see cref="ExecucaoPipeline"/> — registro de rastreabilidade, uma
/// linha por rodada do pipeline de IA sobre uma Release (histórico completo, uma
/// Release pode ter várias). A relação em si é declarada em
/// <see cref="ReleaseConfiguration"/>, porque a coleção vive no agregado
/// (<c>Release.Execucoes</c>) — é o domínio que garante que toda rodada deixa rastro,
/// não a camada de aplicação lembrar de gravar o log.
/// </summary>
public sealed class ExecucaoPipelineConfiguration : IEntityTypeConfiguration<ExecucaoPipeline>
{
    public void Configure(EntityTypeBuilder<ExecucaoPipeline> builder)
    {
        builder.ToTable("execucoes_pipeline", t => t.HasCheckConstraint(
            "ck_execucoes_pipeline_status",
            $"""status IN ('{string.Join("','", Enum.GetValues<StatusExecucaoPipeline>().Select(s => s.ParaValor()))}')"""));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

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
