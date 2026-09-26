using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento de <see cref="ItemComunicado"/> — sempre filho de uma
/// <see cref="VersaoComunicado"/> (FK "VersaoComunicadoId" shadow, configurada em
/// <see cref="VersaoComunicadoConfiguration"/>), não da Release direto: o texto de
/// cada item varia por público-alvo.
///
/// <see cref="ItemComunicado.Origens"/> guarda as CHAVES Jira que originaram o item
/// (agrupamento semântico do estágio 3 do pipeline) como <c>text[]</c>, não como FK
/// para <see cref="HistoriaJira"/>: é exatamente o que o domínio expõe hoje, e criar
/// uma tabela de junção normalizada exigiria mudar a assinatura do domínio (Origens
/// deixaria de ser lista de string) sem ganho real — a chave já é validada na origem
/// (resposta do LLM sobre histórias que vieram do Jira). Índice GIN cobre consultas
/// tipo "quais itens vieram da história X".
/// </summary>
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

        // Nasce true: o revisor exclui o que não é relevante, não seleciona o que é.
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

        // Deriva Texto/TextoEditadoManualmente — não persiste.
        builder.Ignore(i => i.TextoFinal);
    }

    private static CategoriaAlteracao ParseCategoria(string valor)
    {
        CategoriaAlteracaoExtensions.TentarConverter(valor, out var categoria);
        return categoria;
    }
}
