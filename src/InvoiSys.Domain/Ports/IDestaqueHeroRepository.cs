using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IDestaqueHeroRepository
{
    Task<IReadOnlyList<DestaqueHero>> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
