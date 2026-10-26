using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class TokenRedefinicaoSenhaConfiguration : IEntityTypeConfiguration<TokenRedefinicaoSenha>
{
    public void Configure(EntityTypeBuilder<TokenRedefinicaoSenha> builder)
    {
        builder.ToTable("tokens_redefinicao_senha");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedNever();

        builder.Property(t => t.UsuarioId).IsRequired();
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.UsuarioId);

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.CriadoEm).IsRequired().HasDefaultValueSql("now()");
        builder.Property(t => t.ExpiraEm).IsRequired();
        builder.Property(t => t.UsadoEm);
    }
}
