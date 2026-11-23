using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed record VersaoComStatus(VersaoJira Versao, StatusPipeline? StatusInvoisys);

public sealed class ConsultaVersoesJira(IJiraClient jira, IReleaseRepository releases)
{
    public async Task<IReadOnlyList<VersaoComStatus>> ListarAsync(
        bool incluirLancadas,
        CancellationToken cancellationToken = default)
    {
        var versoes = (await jira.ListarVersoesAsync(cancellationToken))
            .Where(v => !v.Arquivada && (incluirLancadas || !v.Lancada))
            .ToList();

        var status = await releases.BuscarStatusPorChavesAsync(
            [.. versoes.Select(v => v.Nome)],
            cancellationToken);

        return [.. versoes
            .OrderBy(v => v.Lancada)
            .ThenByDescending(v => v.DataLancamento ?? DateOnly.MinValue)
            .Select(v => new VersaoComStatus(
                v,
                status.TryGetValue(v.Nome, out var s) ? s : null))];
    }
}
