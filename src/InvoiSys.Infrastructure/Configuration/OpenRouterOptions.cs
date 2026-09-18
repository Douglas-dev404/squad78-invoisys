namespace InvoiSys.Infrastructure.Configuration;

/// <summary>
/// Configuração do provider de LLM — OpenRouter (gateway único, mesmo schema de
/// request da OpenAI). Ligada à seção "OpenRouter".
/// </summary>
public sealed class OpenRouterOptions
{
    public const string SecaoConfig = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Formato provedor/modelo, ex: openai/gpt-4o-mini, anthropic/claude-3.5-sonnet.
    /// </summary>
    public string Modelo { get; set; } = "openai/gpt-4o-mini";

    /// <summary>
    /// Modelos alternativos, usados pela OpenRouter em erro 5xx/rate limit — fallback
    /// nativo do gateway, sem round-trip nosso.
    /// </summary>
    public List<string> ModelosFallback { get; set; } = [];

    /// <summary>
    /// Endpoint de chat completions. Configurável para permitir apontar a um gateway
    /// compatível ou a um servidor de teste — o padrão é a OpenRouter.
    /// </summary>
    public string Endpoint { get; set; } = "https://openrouter.ai/api/v1/chat/completions";

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(ApiKey);
}
