using InvoiSys.Domain.Entities;
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

    public Task SalvarAsync(Release release, CancellationToken cancellationToken = default)
    {
        _porId[release.Id] = release;
        _porChaveJira[release.ChaveJira] = release;
        return Task.CompletedTask;
    }
}
