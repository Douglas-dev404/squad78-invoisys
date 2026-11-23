using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class EnvioComunicadoRepository(InvoiSysDbContext contexto) : IEnvioComunicadoRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task AdicionarAsync(
        IReadOnlyCollection<EnvioComunicado> envios,
        CancellationToken cancellationToken = default)
    {
        _contexto.EnviosComunicado.AddRange(envios);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnvioComunicado>> ListarPorExportacaoAsync(
        Guid comunicadoExportadoId,
        CancellationToken cancellationToken = default) =>
        await _contexto.EnviosComunicado
            .AsNoTracking()
            .Where(e => e.ComunicadoExportadoId == comunicadoExportadoId)
            .OrderBy(e => e.Nome)
            .ThenBy(e => e.Email)
            .ToListAsync(cancellationToken);
}
