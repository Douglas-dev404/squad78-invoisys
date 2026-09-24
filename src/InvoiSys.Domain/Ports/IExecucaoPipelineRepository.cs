using InvoiSys.Domain.Entities;

public interface IExecucaoPipelineRepository
{
    Task AdicionarAsync(ExecucaoPipeline execucao, CancellationToken ct = default);
    Task AtualizarAsync(ExecucaoPipeline execucao, CancellationToken ct = default);
    Task<ExecucaoPipeline?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExecucaoPipeline>> ObterPorReleaseIdAsync(Guid releaseId, CancellationToken ct = default);
}