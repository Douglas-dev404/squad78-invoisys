using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento de <see cref="ComunicadoExportado"/> — uma linha por exportação, sem
/// unique constraint na combinação: reexportar é um evento novo, o histórico completo
/// é o que importa para auditoria.
///
/// Aponta para a <see cref="VersaoComunicado"/> que originou o arquivo, e guarda
/// <c>release_id</c> junto por conveniência de consulta. A FK da versão é
/// <c>Restrict</c>: apagar uma versão que já gerou export publicado reescreveria o
/// histórico de auditoria.
/// </summary>
public sealed class ComunicadoExportadoConfiguration : IEntityTypeConfiguration<ComunicadoExportado>
{
    public void Configure(EntityTypeBuilder<ComunicadoExportado> builder)
    {
        builder.ToTable("comunicados_exportados", t =>
        {
            t.HasCheckConstraint(
                "ck_comunicados_exportados_publico",
                $"""publico IN ('{string.Join("','", Enum.GetValues<PublicoAlvo>().Select(p => p.ParaValor()))}')""");
            t.HasCheckConstraint(
                "ck_comunicados_exportados_formato",
                $"""formato IN ('{string.Join("','", Enum.GetValues<FormatoExportacao>().Select(f => f.ParaValor()))}')""");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(c => c.ReleaseId).IsRequired();
        builder.HasOne<Release>()
            .WithMany()
            .HasForeignKey(c => c.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.ReleaseId);

        builder.Property(c => c.VersaoComunicadoId).IsRequired();
        builder.HasOne<VersaoComunicado>()
            .WithMany()
            .HasForeignKey(c => c.VersaoComunicadoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.VersaoComunicadoId);

        builder.Property(c => c.Publico)
            .HasConversion(publico => publico.ParaValor(), valor => ParsePublico(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Formato)
            .HasConversion(formato => formato.ParaValor(), valor => ParseFormato(valor))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Conteudo).HasColumnType("text");
        builder.Property(c => c.CaminhoArquivo).HasColumnType("text");
        builder.Property(c => c.GeradoPor).HasMaxLength(200);
        builder.Property(c => c.GeradoEm).IsRequired();
    }

    private static PublicoAlvo ParsePublico(string valor) =>
        Enum.GetValues<PublicoAlvo>().First(p => p.ParaValor() == valor);

    private static FormatoExportacao ParseFormato(string valor) =>
        Enum.GetValues<FormatoExportacao>().First(f => f.ParaValor() == valor);
}
