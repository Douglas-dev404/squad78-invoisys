using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Database.Configurations;

public sealed class ConfiguracaoIntegracaoConfiguration : IEntityTypeConfiguration<ConfiguracaoIntegracao>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoIntegracao> builder)
    {
        builder.ToTable("configuracoes_integracao");

        builder.HasKey(c => c.Chave);
        builder.Property(c => c.Chave).HasMaxLength(100).ValueGeneratedNever();
        builder.Property(c => c.Valor).HasColumnType("text").IsRequired();
        builder.Property(c => c.Secreto).IsRequired();
        builder.Property(c => c.AtualizadoEm).HasDefaultValueSql("now()");
    }
}
