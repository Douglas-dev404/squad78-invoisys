using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Infrastructure.Jira;

public sealed class JiraClientPendente : IJiraClient
{
    private const string Mensagem =
        "Jira não configurado — defina Jira:BaseUrl, Jira:Email, Jira:ApiToken e Jira:ProjectKey "
        + "(ou as variáveis de ambiente Jira__BaseUrl, Jira__Email, Jira__ApiToken e "
        + "Jira__ProjectKey).";

    public Task<IReadOnlyList<VersaoJira>> ListarVersoesDoProjetoAsync(
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);

    public Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);
}
