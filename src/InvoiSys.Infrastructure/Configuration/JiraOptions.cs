namespace InvoiSys.Infrastructure.Configuration;

public sealed class JiraOptions
{
    public const string SecaoConfig = "Jira";

    public string BaseUrl { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(ApiToken);
}
