using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IReleaseRepository
{
    Task<Release?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(Release release, CancellationToken cancellationToken = default);
}
