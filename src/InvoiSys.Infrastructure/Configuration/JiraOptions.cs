namespace InvoiSys.Infrastructure.Configuration;

/// <summary>
/// Configuração do Jira, ligada à seção "Jira" do appsettings/variáveis de ambiente.
/// Única porta de entrada dessa config — nenhuma camada lê variável de ambiente direto.
/// </summary>
public sealed class JiraOptions
{
    public const string SecaoConfig = "Jira";

    /// <summary>Ex: https://suaempresa.atlassian.net</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>API token, não senha — Basic Auth da Jira Cloud exige token.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(ApiToken);
}
