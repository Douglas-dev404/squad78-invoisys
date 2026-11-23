using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IJiraClient
{
    Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VersaoJira>> ListarVersoesAsync(CancellationToken cancellationToken = default);
}
