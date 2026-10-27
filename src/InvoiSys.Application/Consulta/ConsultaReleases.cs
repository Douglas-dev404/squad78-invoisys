using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed class ConsultaReleases(IReleaseRepository releases)
{
    public Task<IReadOnlyList<ReleaseResumo>> ListarAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default) =>
        releases.ListarResumosAsync(status, cancellationToken);

    public async Task<Release> DetalharAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default) =>
        await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);
}
