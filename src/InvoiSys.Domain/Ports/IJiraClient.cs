using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IJiraClient
{
    Task<IReadOnlyList<VersaoJira>> ListarVersoesDoProjetoAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default);
}

public sealed record VersaoJira(
    string Nome,
    bool Lancada,
    bool Arquivada,
    DateOnly? DataLancamento);
