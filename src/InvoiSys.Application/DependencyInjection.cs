using InvoiSys.Application.Autenticacao;
using InvoiSys.Application.Consulta;
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

        services.AddScoped(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var cooldown = int.TryParse(config["Auth:RecuperacaoSenha:CooldownSegundos"], out var segundos)
                ? segundos
                : 60;
            var opcoes = new OpcoesRecuperacaoSenha(
                config["Auth:RecuperacaoSenha:UrlBase"] ?? string.Empty,
                TimeSpan.FromSeconds(cooldown));

            return new RecuperacaoSenha(
                provider.GetRequiredService<IUsuarioRepository>(),
                provider.GetRequiredService<ITokenRedefinicaoSenhaRepository>(),
                provider.GetRequiredService<IHashSenha>(),
                provider.GetRequiredService<IEnvioEmail>(),
                opcoes,
                provider.GetRequiredService<TimeProvider>());
        });

        services.AddScoped<RevisaoComunicado>();
        services.AddScoped<ConsultaReleases>();

        return services;
    }
}
