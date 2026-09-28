using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IReleaseRepository"/>: guarda as Releases salvas em memória, por
/// Id e por ChaveJira. Sem distinção Add/Update do EF — devolve a mesma instância salva
/// quando buscada de novo, o suficiente para testar o pipeline sem um banco real.
/// </summary>
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
