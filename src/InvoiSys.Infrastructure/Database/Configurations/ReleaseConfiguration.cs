using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases", t => t.HasCheckConstraint(
            "ck_releases_status",
            $"""status IN ('{string.Join("','", ValoresDe<StatusPipeline>(s => s.ParaValor()))}')"""));

        builder.HasKey(r => r.Id);

        // Id nasce no domínio: sem ValueGeneratedNever, filho novo numa Release carregada vira UPDATE de linha inexistente (ADR-010).
        builder.Property(r => r.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(r => r.ChaveJira)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(r => r.ChaveJira).IsUnique();

        builder.Property(r => r.Status)
            .HasConversion(
                status => status.ParaValor(),
                valor => ParseStatus(valor))
            .HasMaxLength(30)
            .IsRequired();
        builder.HasIndex(r => r.Status);

        builder.Property<DateTimeOffset>("CriadoEm").HasDefaultValueSql("now()");
        builder.Property<DateTimeOffset>("AtualizadoEm").HasDefaultValueSql("now()");

        builder.HasMany(r => r.Historias)
            .WithOne()
            .HasForeignKey("ReleaseId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Historias)
            .HasField("_historias")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Versoes)
            .WithOne()
            .HasForeignKey(v => v.ReleaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Versoes)
            .HasField("_versoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Execucoes)
            .WithOne()
            .HasForeignKey(e => e.ReleaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Execucoes)
            .HasField("_execucoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.ProntaParaExportar);
        builder.Ignore(r => r.VersaoCliente);
        builder.Ignore(r => r.TituloExecutivo);
        builder.Ignore(r => r.ResumoExecutivo);
        builder.Ignore(r => r.Itens);
        builder.Ignore(r => r.AprovadoPor);
        builder.Ignore(r => r.AprovadoEm);
    }

    private static StatusPipeline ParseStatus(string valor) =>
        Enum.GetValues<StatusPipeline>().First(s => s.ParaValor() == valor);

    private static IEnumerable<string> ValoresDe<TEnum>(Func<TEnum, string> paraValor)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(paraValor);
}
