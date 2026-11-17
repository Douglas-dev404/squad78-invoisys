namespace InvoiSys.Infrastructure.Configuration;

public sealed class JiraOptions
{
    public const string SecaoConfig = "Jira";

    public string BaseUrl { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    public string Projeto { get; set; } = string.Empty;

    public JiraReleaseNoteOptions ReleaseNote { get; set; } = new();

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(ApiToken)
        && !string.IsNullOrWhiteSpace(Projeto);
}

public sealed class JiraReleaseNoteOptions
{
    public const string TipoSubtarefaPadrao = "Release Note";

    public string TipoSubtarefa { get; set; } = TipoSubtarefaPadrao;

    public string CampoCustomizado { get; set; } = string.Empty;
}
