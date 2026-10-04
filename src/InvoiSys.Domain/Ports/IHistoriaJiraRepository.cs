using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IHistoriaJiraRepository
{
    Task<HistoriaJira?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HistoriaJira?> BuscarPorChaveAsync(
        Guid releaseId,
        string chave,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoriaJira>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);
}
