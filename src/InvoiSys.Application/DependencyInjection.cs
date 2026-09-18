using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Application;

/// <summary>
/// Registro dos serviços de aplicação. Fica aqui, e não no composition root da
/// infraestrutura, porque <c>InvoiSys.Infrastructure</c> não referencia
/// <c>InvoiSys.Application</c> — a dependência é a outra: a API conhece as duas.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scoped porque depende de IJiraClient e ILlmProvider, que também são scoped:
        // registrar como singleton capturaria as portas de um escopo já encerrado.
        services.AddScoped(provider => new PipelineGeracaoReleaseNote(
            provider.GetRequiredService<IJiraClient>(),
            provider.GetRequiredService<ILlmProvider>(),
            // Lido da configuração em vez de IOptions<OpenRouterOptions>: essa classe
            // vive em InvoiSys.Infrastructure, e a camada de aplicação não a referencia.
            // O modelo é só um rótulo para o log de execução — sem isso a coluna
            // modelo_llm gravaria vazio e a rastreabilidade perderia o dado mais útil
            // para investigar um comunicado ruim.
            configuration["OpenRouter:Modelo"]));

        return services;
    }
}
