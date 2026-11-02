using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class HistoricoRevisaoRepository(InvoiSysDbContext contexto) : IHistoricoRevisaoRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<IReadOnlyList<EventoRevisaoResumo>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default) =>
        await (
            from e in _contexto.EventosRevisao.AsNoTracking()
            where e.ReleaseId == releaseId
            join u in _contexto.Usuarios on e.UsuarioId equals u.Id
            join v in _contexto.VersoesComunicado on e.VersaoId equals v.Id
            orderby e.Sequencia
            select new EventoRevisaoResumo(
                e.Id,
                e.Acao,
                v.Publico,
                e.ItemId,
                e.UsuarioId,
                u.Nome,
                e.OcorridoEm,
                e.Detalhe))
        .ToListAsync(cancellationToken);
}
