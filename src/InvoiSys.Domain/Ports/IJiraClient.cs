using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta para acesso ao Jira. Desacopla o domínio/services do SDK/HTTP concreto — a
/// implementação real (HttpClient + REST API v3) vive em InvoiSys.Infrastructure.Jira.
///
/// Contrato verificado contra a documentação oficial atualizada em 2026-08-24
/// (endpoint /rest/api/3/search foi removido, substituído por /rest/api/3/search/jql;
/// paginação por nextPageToken). Ver fontes em documentação interna de estado do projeto.
/// </summary>
public interface IJiraClient
{
    /// <summary>
    /// Busca todas as issues associadas à Release (fixVersion) e suas subtarefas do
    /// tipo Release Note, já traduzidas para <see cref="HistoriaJira"/> — incluindo o
    /// texto da subtarefa Release Note quando existir. Trata paginação internamente
    /// (nextPageToken), o chamador recebe a lista completa.
    /// </summary>
    Task<IReadOnlyList<HistoriaJira>> BuscarHistoriasDaReleaseAsync(
        string fixVersion,
        CancellationToken cancellationToken = default);
}
