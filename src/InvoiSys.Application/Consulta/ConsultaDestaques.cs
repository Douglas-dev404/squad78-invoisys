using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed class ConsultaDestaques(IDestaqueHeroRepository destaques)
{
    public Task<IReadOnlyList<DestaqueHero>> ListarAsync(CancellationToken cancellationToken = default) =>
        destaques.ListarAtivosAsync(cancellationToken);
}
