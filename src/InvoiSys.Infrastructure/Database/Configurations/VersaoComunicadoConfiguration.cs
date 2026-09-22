using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento de <see cref="VersaoComunicado"/> — o comunicado de uma Release escrito
/// para um público específico, com seu próprio ciclo de revisão.
///
/// Unique em <c>(release_id, publico)</c>: uma Release tem no máximo uma versão viva
/// por audiência. Reprocessar um público substitui o conteúdo da versão existente em
/// vez de criar outra — o histórico do que já foi publicado vive em
/// <see cref="ComunicadoExportado"/>, não em versões duplicadas.
/// </summary>
public sealed class VersaoComunicadoConfiguration : IEntityTypeConfiguration<VersaoComunicado>
{
    public void Configure(EntityTypeBuilder<VersaoComunicado> builder)
    {
        builder.ToTable("versoes_comunicado", t =>
        {
            t.HasCheckConstraint(
                "ck_versoes_comunicado_publico",
                $"""publico IN ('{string.Join("','", Enum.GetValues<PublicoAlvo>().Select(p => p.ParaValor()))}')""");
            t.HasCheckConstraint(
                "ck_versoes_comunicado_status",
                $"""status IN ('{string.Join("','", Enum.GetValues<StatusRevisao>().Select(s => s.ParaValor()))}')""");

            // Reprovar sem motivo é inconsistência de dados, não só de código: o
            // domínio já bloqueia, o banco garante mesmo para INSERT via SQL direto.
            t.HasCheckConstraint(
                "ck_versoes_comunicado_motivo_reprovacao",
                "status <> 'reprovado' OR motivo_reprovacao IS NOT NULL");
        });

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(v => v.ReleaseId).IsRequired();
        builder.HasIndex(v => new { v.ReleaseId, v.Publico }).IsUnique();

        builder.Property(v => v.Publico)
            .HasConversion(publico => publico.ParaValor(), valor => ParsePublico(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion(status => status.ParaValor(), valor => ParseStatus(valor))
            .HasMaxLength(30)
            .IsRequired();
        builder.HasIndex(v => v.Status);

        builder.Property(v => v.TituloExecutivo).HasColumnType("text");
        builder.Property(v => v.ResumoExecutivo).HasColumnType("text");

        // Texto livre, não FK para Usuario: autenticação ainda não está implementada
        // no domínio. Quando entrar, este campo migra para RevisadoPorId (uuid).
        builder.Property(v => v.RevisadoPor).HasMaxLength(200);
        builder.Property(v => v.RevisadoEm);
        builder.Property(v => v.MotivoReprovacao).HasColumnType("text");
        builder.Property(v => v.CriadoEm).IsRequired();

        builder.Property<DateTimeOffset>("AtualizadoEm").HasDefaultValueSql("now()");

        builder.HasMany(v => v.Itens)
            .WithOne()
            .HasForeignKey("VersaoComunicadoId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.Itens)
            .HasField("_itens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Derivadas dos itens e do status — sem coluna própria.
        builder.Ignore(v => v.ItensPublicaveis);
        builder.Ignore(v => v.ProntaParaExportar);
    }

    private static PublicoAlvo ParsePublico(string valor) =>
        Enum.GetValues<PublicoAlvo>().First(p => p.ParaValor() == valor);

    private static StatusRevisao ParseStatus(string valor) =>
        Enum.GetValues<StatusRevisao>().First(s => s.ParaValor() == valor);
}
