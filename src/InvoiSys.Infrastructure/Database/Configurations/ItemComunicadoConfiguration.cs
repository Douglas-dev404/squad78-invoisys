using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class ItemComunicadoConfiguration : IEntityTypeConfiguration<ItemComunicado>
{
    public void Configure(EntityTypeBuilder<ItemComunicado> builder)
    {
        builder.ToTable("itens_comunicado", t => t.HasCheckConstraint(
            "ck_itens_comunicado_categoria",
            $"""categoria IN ('{string.Join("','", Enum.GetValues<CategoriaAlteracao>().Select(c => c.ParaValor()))}')"""));

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(i => i.Categoria)
            .HasConversion(
                categoria => categoria.ParaValor(),
                valor => ParseCategoria(valor))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Texto).HasColumnType("text").IsRequired();
        builder.Property(i => i.TextoEditadoManualmente).HasColumnType("text");

        builder.Property(i => i.Incluido).HasDefaultValue(true).IsRequired();
        builder.Property(i => i.MotivoExclusao).HasColumnType("text");

        builder.Property(i => i.Origens)
            .HasColumnType("text[]")
            .HasConversion(
                lista => lista.ToArray(),
                array => (IReadOnlyList<string>)array.ToList())
            .Metadata.SetValueComparer(ArrayConversionHelper.ListaStringComparer);
        builder.HasIndex(i => i.Origens).HasMethod("GIN");

        builder.Property<DateTimeOffset>("CriadoEm").HasDefaultValueSql("now()");
        builder.Property<DateTimeOffset>("AtualizadoEm").HasDefaultValueSql("now()");

        builder.Ignore(i => i.TextoFinal);
    }

    private static CategoriaAlteracao ParseCategoria(string valor)
    {
        CategoriaAlteracaoExtensions.TentarConverter(valor, out var categoria);
        return categoria;
    }
}
