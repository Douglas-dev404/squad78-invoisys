namespace InvoiSys.Infrastructure.Configuration;

public sealed class OpenRouterOptions
{
    public const string SecaoConfig = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;

    public string Modelo { get; set; } = "openai/gpt-4o-mini";

    public List<string> ModelosFallback { get; set; } = [];

    public string Endpoint { get; set; } = "https://openrouter.ai/api/v1/chat/completions";

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ApiKey);
}
