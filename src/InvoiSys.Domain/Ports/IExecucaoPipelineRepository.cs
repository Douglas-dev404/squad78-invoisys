using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IExecucaoPipelineRepository
{
    Task<ExecucaoPipeline?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecucaoPipeline>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);
}
