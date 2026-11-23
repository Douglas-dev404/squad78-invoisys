using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeJiraClient : IJiraClient
{
    public List<HistoriaJira> Historias { get; set; } = [];

    public Exception? FalhaAoBuscar { get; set; }

    public List<string> ChavesConsultadas { get; } = [];

    public List<VersaoJira> Versoes { get; set; } = [];

    public Task<IReadOnlyList<VersaoJira>> ListarVersoesDoProjetoAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VersaoJira>>(Versoes);

    public Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default)
    {
        ChavesConsultadas.Add(fixVersion);

        if (FalhaAoBuscar is not null)
        {
            throw FalhaAoBuscar;
        }

        return Task.FromResult<IReadOnlyList<HistoriaJira>>(Historias);
    }
}
