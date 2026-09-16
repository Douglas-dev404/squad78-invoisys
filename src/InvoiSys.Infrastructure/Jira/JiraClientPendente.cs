using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Llm;

namespace InvoiSys.Infrastructure.Jira;

/// <summary>
/// Adapter placeholder de <see cref="IJiraClient"/>, usado quando não há credencial de
/// Jira configurada.
///
/// Simétrico ao <see cref="ProviderPendente"/> do LLM, e pelo mesmo motivo: sem
/// BaseUrl o HttpClient estoura InvalidOperationException lá no fundo do adapter, e o
/// usuário recebe um 500 com stack trace em vez da causa real. Falhar aqui, com
/// mensagem que diz o que configurar, vira um 501 honesto.
/// </summary>
public sealed class JiraClientPendente : IJiraClient
{
    private const string Mensagem =
        "Jira não configurado — defina Jira:BaseUrl, Jira:Email e Jira:ApiToken (ou as "
        + "variáveis de ambiente Jira__BaseUrl, Jira__Email, Jira__ApiToken).";

    public Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);
}
