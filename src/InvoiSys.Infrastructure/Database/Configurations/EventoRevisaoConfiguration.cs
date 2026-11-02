using System.Text.Json;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class EventoRevisaoConfiguration : IEntityTypeConfiguration<EventoRevisao>
{
    public void Configure(EntityTypeBuilder<EventoRevisao> builder)
    {
        builder.ToTable("eventos_revisao", t => t.HasCheckConstraint(
            "ck_eventos_revisao_acao",
            $"""acao IN ('{string.Join("','", Enum.GetValues<AcaoRevisao>().Select(a => a.ParaValor()))}')"""));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(e => e.Sequencia).UseIdentityAlwaysColumn();

        builder.Property(e => e.ReleaseId).IsRequired();
        builder.HasOne<Release>()
            .WithMany()
            .HasForeignKey(e => e.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.ReleaseId, e.Sequencia });

        builder.Property(e => e.VersaoId).IsRequired();
        builder.HasOne<VersaoComunicado>()
            .WithMany()
            .HasForeignKey(e => e.VersaoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.VersaoId);

        builder.Property(e => e.ItemId);

        builder.Property(e => e.Acao)
            .HasConversion(acao => acao.ParaValor(), valor => ParseAcao(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.UsuarioId).IsRequired();
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.UsuarioId);

        builder.Property(e => e.OcorridoEm).IsRequired();

        builder.Property(e => e.Detalhe)
            .HasColumnType("jsonb")
            .HasConversion(
                new ValueConverter<IReadOnlyDictionary<string, object?>, string>(
                    detalhe => JsonSerializer.Serialize(detalhe, (JsonSerializerOptions?)null),
                    json => Desserializar(json)),
                new ValueComparer<IReadOnlyDictionary<string, object?>>(
                    (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
                        == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                    d => JsonSerializer.Serialize(d, (JsonSerializerOptions?)null).GetHashCode(),
                    d => d));
    }

    private static IReadOnlyDictionary<string, object?> Desserializar(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];

    private static AcaoRevisao ParseAcao(string valor) =>
        Enum.GetValues<AcaoRevisao>().First(a => a.ParaValor() == valor);
}
