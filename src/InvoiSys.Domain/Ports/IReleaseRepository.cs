using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface IReleaseRepository
{
    Task<Release?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(Release release, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseResumo>> ListarResumosAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default);

    Task<DashboardDados> BuscarDashboardAsync(
        DateOnly hojeUtc,
        CancellationToken cancellationToken = default);
}

public sealed record ReleaseResumo(string ChaveJira, StatusPipeline Status, DateTimeOffset CriadoEm);

public sealed record DashboardDados(
    int Aprovadas,
    int AguardandoRevisao,
    int Falharam,
    int Exportacoes,
    int ItensGerados,
    int ItensEditados,
    int ItensExcluidos,
    IReadOnlyList<DashboardDia> UltimosDias,
    IReadOnlyList<DashboardReleaseRecente> Recentes);

public sealed record DashboardDia(DateOnly Data, int Aprovadas, int Geradas);

public sealed record DashboardReleaseRecente(
    string ChaveJira,
    StatusPipeline Status,
    string? TituloExecutivo,
    DateTimeOffset CriadoEm);
