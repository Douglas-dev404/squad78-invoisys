using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Nome).HasMaxLength(200).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        // bcrypt/argon2 — nunca a senha em texto plano.
        builder.Property(u => u.SenhaHash).HasMaxLength(255).IsRequired();

        builder.Property(u => u.Papel).HasMaxLength(50).IsRequired().HasDefaultValue("release_manager");

        builder.Property(u => u.AvatarUrl).HasColumnType("text");

        builder.Property(u => u.Ativo).IsRequired().HasDefaultValue(true);

        builder.Property(u => u.CriadoEm).HasDefaultValueSql("now()");
    }
}
