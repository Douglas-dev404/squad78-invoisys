using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeComunicadoExportadoRepository : IComunicadoExportadoRepository
{
    private readonly List<ComunicadoExportado> _exportados = [];

    public IReadOnlyList<ComunicadoExportado> Exportados => _exportados;

    public Task AdicionarAsync(
        ComunicadoExportado exportado,
        CancellationToken cancellationToken = default)
    {
        _exportados.Add(exportado);
        return Task.CompletedTask;
    }

    public Task<ComunicadoExportado?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_exportados.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ComunicadoExportado>>([.. _exportados
            .Where(e => e.ReleaseId == releaseId && (publico is null || e.Publico == publico))
            .OrderByDescending(e => e.GeradoEm)]);
}
