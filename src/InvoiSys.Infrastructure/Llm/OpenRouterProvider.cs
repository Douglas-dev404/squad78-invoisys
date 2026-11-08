using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Llm;

public sealed class OpenRouterProvider(
    HttpClient http,
    IOptions<OpenRouterOptions> options,
    PromptLoader prompts,
    ILogger<OpenRouterProvider> logger) : ChatCompletionsLlmProvider(http, prompts, logger)
{
    private readonly OpenRouterOptions _opcoes = options.Value;

    protected override string NomeProvedor => "OpenRouter";

    protected override string Endpoint => _opcoes.Endpoint;

    protected override string Modelo => _opcoes.Modelo;

    protected override IReadOnlyList<string> ModelosFallback => _opcoes.ModelosFallback;
}
