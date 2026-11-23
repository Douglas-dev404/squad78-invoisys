using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed class ConsultaVersoesJira(IJiraClient jiraClient)
{
    public Task<IReadOnlyList<VersaoJira>> ListarAsync(
        CancellationToken cancellationToken = default) =>
        jiraClient.ListarVersoesDoProjetoAsync(cancellationToken);
}
