using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IEnvioComunicadoRepository
{
    Task AdicionarAsync(IReadOnlyCollection<EnvioComunicado> envios, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvioComunicado>> ListarPorExportacaoAsync(
        Guid comunicadoExportadoId,
        CancellationToken cancellationToken = default);
}
