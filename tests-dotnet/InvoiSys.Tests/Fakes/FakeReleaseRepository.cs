using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeReleaseRepository : IReleaseRepository
{
    private readonly Dictionary<Guid, Release> _porId = [];
    private readonly Dictionary<string, Release> _porChaveJira = [];

    public Task<Release?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_porChaveJira.GetValueOrDefault(chaveJira));

    private readonly Dictionary<Guid, DateTimeOffset> _criadoEm = [];

    public Task SalvarAsync(Release release, CancellationToken cancellationToken = default)
    {
        _porId[release.Id] = release;
        _porChaveJira[release.ChaveJira] = release;
        _criadoEm.TryAdd(release.Id, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReleaseResumo>> ListarResumosAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Release> releases = _porId.Values;

        if (status is not null)
        {
            releases = releases.Where(r => r.Status == status);
        }

        return Task.FromResult<IReadOnlyList<ReleaseResumo>>([.. releases
            .Select(r => new ReleaseResumo(r.ChaveJira, r.Status, _criadoEm[r.Id]))
            .OrderByDescending(r => r.CriadoEm)]);
    }
}
