using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IComunicadoExportadoRepository"/> via EF Core.
///
/// Append-only: <see cref="AdicionarAsync"/> sempre faz INSERT (não passa pelo "Add ou
/// Update pelo estado" de <see cref="ReleaseRepository"/>, porque exportação nunca é
/// atualizada) e as consultas são <c>AsNoTracking</c> — não há o que alterar depois.
/// </summary>
public sealed class ComunicadoExportadoRepository(InvoiSysDbContext contexto) : IComunicadoExportadoRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task AdicionarAsync(
        ComunicadoExportado exportado,
        CancellationToken cancellationToken = default)
    {
        _contexto.ComunicadosExportados.Add(exportado);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComunicadoExportado?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.ComunicadosExportados
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.ComunicadosExportados
            .AsNoTracking()
            .Where(c => c.ReleaseId == releaseId);

        if (publico is not null)
        {
            consulta = consulta.Where(c => c.Publico == publico);
        }

        return await consulta
            .OrderByDescending(c => c.GeradoEm)
            .ToListAsync(cancellationToken);
    }
}
