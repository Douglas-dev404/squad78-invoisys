using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed class ConsultaDashboard(IReleaseRepository releases, TimeProvider relogio)
{
    public Task<DashboardDados> BuscarAsync(CancellationToken cancellationToken = default) =>
        releases.BuscarDashboardAsync(
            DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime),
            cancellationToken);
}
