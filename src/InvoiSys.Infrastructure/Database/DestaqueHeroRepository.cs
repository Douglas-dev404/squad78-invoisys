using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class DestaqueHeroRepository(InvoiSysDbContext contexto) : IDestaqueHeroRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<IReadOnlyList<DestaqueHero>> ListarAtivosAsync(
        CancellationToken cancellationToken = default) =>
        await _contexto.DestaquesHero
            .AsNoTracking()
            .Where(d => d.Ativo)
            .OrderBy(d => d.Ordem)
            .ToListAsync(cancellationToken);
}
