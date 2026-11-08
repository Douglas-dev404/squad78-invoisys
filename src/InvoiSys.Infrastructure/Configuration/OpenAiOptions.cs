namespace InvoiSys.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string SecaoConfig = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;

    public string Modelo { get; set; } = "gpt-4o-mini";

    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ApiKey);
}
