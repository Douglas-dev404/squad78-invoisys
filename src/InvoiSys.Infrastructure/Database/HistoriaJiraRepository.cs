using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IHistoriaJiraRepository"/> via EF Core: leitura de
/// <see cref="HistoriaJira"/> sem materializar o agregado <see cref="Release"/>.
///
/// Todas as consultas usam <c>AsNoTracking</c> — ao contrário de
/// <see cref="ReleaseRepository"/>, que rastreia de propósito porque decide Add/Update
/// pelo estado da entidade. Aqui a porta é somente leitura, então rastrear só custaria
/// memória e abriria a chance de alguém salvar a história por fora do agregado.
/// </summary>
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

    /// <summary>
    /// A FK para a Release é shadow property ("ReleaseId", configurada em
    /// <c>ReleaseConfiguration</c>) — o domínio não a expõe, então o filtro por Release
    /// só é possível via <c>EF.Property</c>.
    /// </summary>
    private IQueryable<HistoriaJira> HistoriasDaRelease(Guid releaseId) =>
        _contexto.HistoriasJira
            .AsNoTracking()
            .Where(h => EF.Property<Guid>(h, "ReleaseId") == releaseId);
}
