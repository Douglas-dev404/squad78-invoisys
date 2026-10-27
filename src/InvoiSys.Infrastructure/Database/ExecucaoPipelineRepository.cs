using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ExecucaoPipelineRepository(InvoiSysDbContext contexto) : IExecucaoPipelineRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<ExecucaoPipeline?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.ExecucoesPipeline
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ExecucaoPipeline>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default) =>
        await _contexto.ExecucoesPipeline
            .AsNoTracking()
            .Where(e => e.ReleaseId == releaseId)
            .OrderByDescending(e => e.IniciadoEm)
            .ToListAsync(cancellationToken);
}
