using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class HistoriaJiraRepository(InvoiSysDbContext contexto) : IHistoriaJiraRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<HistoriaJira?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.HistoriasJira
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<HistoriaJira?> BuscarPorChaveAsync(
        Guid releaseId,
        string chave,
        CancellationToken cancellationToken = default) =>
        await HistoriasDaRelease(releaseId)
            .FirstOrDefaultAsync(h => h.Chave == chave, cancellationToken);

    public async Task<IReadOnlyList<HistoriaJira>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default) =>
        await HistoriasDaRelease(releaseId)
            .OrderBy(h => h.Chave)
            .ToListAsync(cancellationToken);

    private IQueryable<HistoriaJira> HistoriasDaRelease(Guid releaseId) =>
        _contexto.HistoriasJira
            .AsNoTracking()
            .Where(h => EF.Property<Guid>(h, "ReleaseId") == releaseId);
}
