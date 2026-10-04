using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface IReleaseRepository
{
    Task<Release?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(Release release, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseResumo>> ListarResumosAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default);
}

public sealed record ReleaseResumo(string ChaveJira, StatusPipeline Status, DateTimeOffset CriadoEm);
