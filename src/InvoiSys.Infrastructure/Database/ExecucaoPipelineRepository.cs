using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IExecucaoPipelineRepository"/> via EF Core: leitura
/// do histórico de execuções sem materializar o agregado <see cref="Release"/>.
///
/// <c>AsNoTracking</c> em tudo, como em <see cref="HistoriaJiraRepository"/>: a porta é
/// somente leitura, e rastrear abriria a chance de salvar uma execução por fora do
/// agregado. A ordenação por <c>iniciado_em</c> usa o índice de <c>release_id</c> para o
/// filtro.
/// </summary>
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
