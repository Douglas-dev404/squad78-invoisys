using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

public class ExecucaoPipelineRepository : IExecucaoPipelineRepository
{
    private readonly InvoiSysDbContext _context;

    public ExecucaoPipelineRepository(InvoiSysDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(ExecucaoPipeline execucao, CancellationToken ct = default)
    {
        await _context.ExecucoesPipeline.AddAsync(execucao, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(ExecucaoPipeline execucao, CancellationToken ct = default)
    {
        _context.ExecucoesPipeline.Update(execucao);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ExecucaoPipeline?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ExecucoesPipeline
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<ExecucaoPipeline>> ObterPorReleaseIdAsync(Guid releaseId, CancellationToken ct = default)
    {
        return await _context.ExecucoesPipeline
            .Where(e => e.ReleaseId == releaseId)
            .OrderByDescending(e => e.IniciadoEm)
            .ToListAsync(ct);
    }
}