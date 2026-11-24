namespace InvoiSys.Infrastructure.Integracoes;

public static class ChavesIntegracao
{
    public const string JiraBaseUrl = "jira.base_url";
    public const string JiraEmail = "jira.email";
    public const string JiraApiToken = "jira.api_token";
    public const string JiraProjeto = "jira.projeto";

    public const string IaProvedor = "ia.provedor";
    public const string OpenAiModelo = "ia.openai.modelo";
    public const string OpenAiApiKey = "ia.openai.api_key";
    public const string OpenRouterModelo = "ia.openrouter.modelo";
    public const string OpenRouterApiKey = "ia.openrouter.api_key";

    public static readonly IReadOnlySet<string> Secretas =
        new HashSet<string> { JiraApiToken, OpenAiApiKey, OpenRouterApiKey };

    public static readonly IReadOnlySet<string> Todas = new HashSet<string>
    {
        JiraBaseUrl, JiraEmail, JiraApiToken, JiraProjeto,
        IaProvedor, OpenAiModelo, OpenAiApiKey, OpenRouterModelo, OpenRouterApiKey,
    };
}
