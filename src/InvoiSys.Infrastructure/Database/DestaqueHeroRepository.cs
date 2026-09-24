using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IDestaqueHeroRepository"/> via EF Core.
///
/// Consulta com <c>AsNoTracking</c>: a porta é somente leitura, então rastrear só
/// custaria memória. O filtro e a ordenação batem com o índice
/// <c>(ativo, ordem)</c> de <c>DestaqueHeroConfiguration</c>.
/// </summary>
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
