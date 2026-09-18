using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// DbContext único do projeto — a fronteira de persistência entre o domínio
/// (InvoiSys.Domain) e o PostgreSQL. Cada entidade tem sua própria
/// <c>IEntityTypeConfiguration</c> em <see cref="Configurations"/>; nenhum mapeamento
/// vive aqui além do registro delas, para manter esta classe fina.
/// </summary>
public sealed class InvoiSysDbContext(DbContextOptions<InvoiSysDbContext> options)
    : DbContext(options)
{
    public DbSet<Release> Releases => Set<Release>();

    public DbSet<HistoriaJira> HistoriasJira => Set<HistoriaJira>();

    public DbSet<VersaoComunicado> VersoesComunicado => Set<VersaoComunicado>();

    public DbSet<ItemComunicado> ItensComunicado => Set<ItemComunicado>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<ExecucaoPipeline> ExecucoesPipeline => Set<ExecucaoPipeline>();

    public DbSet<ComunicadoExportado> ComunicadosExportados => Set<ComunicadoExportado>();

    public DbSet<DestaqueHero> DestaquesHero => Set<DestaqueHero>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiSysDbContext).Assembly);
    }
}
