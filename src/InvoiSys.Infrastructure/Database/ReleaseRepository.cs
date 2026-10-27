using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ReleaseRepository(InvoiSysDbContext contexto) : IReleaseRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<Release?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await ConsultaComAgregadoCompleto()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default) =>
        await ConsultaComAgregadoCompleto()
            .FirstOrDefaultAsync(r => r.ChaveJira == chaveJira, cancellationToken);

    public async Task SalvarAsync(Release release, CancellationToken cancellationToken = default)
    {
        if (_contexto.Entry(release).State == EntityState.Detached)
        {
            _contexto.Releases.Add(release);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReleaseResumo>> ListarResumosAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Releases.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            consulta = consulta.Where(r => r.Status == status);
        }

        return await consulta
            .OrderByDescending(r => EF.Property<DateTimeOffset>(r, "CriadoEm"))
            .Select(r => new ReleaseResumo(
                r.ChaveJira,
                r.Status,
                EF.Property<DateTimeOffset>(r, "CriadoEm")))
            .ToListAsync(cancellationToken);
    }

    // Sem lazy loading: faltar um Include devolve coleção vazia em silêncio.
    private IQueryable<Release> ConsultaComAgregadoCompleto() =>
        _contexto.Releases
            .Include(r => r.Historias)
            .Include(r => r.Versoes)
                .ThenInclude(v => v.Itens)
            .Include(r => r.Execucoes);
}
