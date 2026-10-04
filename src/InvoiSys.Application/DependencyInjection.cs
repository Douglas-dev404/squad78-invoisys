using InvoiSys.Application.Pipeline;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Scoped: depende de portas scoped; singleton capturaria um escopo já encerrado.
        services.AddScoped(provider => new PipelineGeracaoReleaseNote(
            provider.GetRequiredService<IJiraClient>(),
            provider.GetRequiredService<ILlmProvider>(),
            provider.GetRequiredService<IReleaseRepository>(),
            configuration["OpenRouter:Modelo"]));

        services.AddScoped<RevisaoComunicado>();

        return services;
    }
}
