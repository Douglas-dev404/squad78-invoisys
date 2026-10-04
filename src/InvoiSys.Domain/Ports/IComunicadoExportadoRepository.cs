using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface IComunicadoExportadoRepository
{
    Task AdicionarAsync(ComunicadoExportado exportado, CancellationToken cancellationToken = default);

    Task<ComunicadoExportado?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default);
}
