using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

/// <summary>
/// Consulta de Releases já persistidas: a fila (listagem leve, por status) e o detalhe
/// (agregado completo, todas as versões por público). Só leitura — nada aqui muda estado.
/// </summary>
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
