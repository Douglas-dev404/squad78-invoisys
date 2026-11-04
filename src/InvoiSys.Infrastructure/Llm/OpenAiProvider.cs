using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Llm;

public sealed class OpenAiProvider(
    HttpClient http,
    IOptions<OpenAiOptions> options,
    PromptLoader prompts,
    ILogger<OpenAiProvider> logger) : ChatCompletionsLlmProvider(http, prompts, logger)
{
    private readonly OpenAiOptions _opcoes = options.Value;

    protected override string NomeProvedor => "OpenAI";

    protected override string Endpoint => _opcoes.Endpoint;

    protected override string Modelo => _opcoes.Modelo;
}
