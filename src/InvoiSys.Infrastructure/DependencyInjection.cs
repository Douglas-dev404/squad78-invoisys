using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Infrastructure.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure;

/// <summary>
/// Composition root: único lugar do projeto onde uma porta é amarrada a um adapter
/// concreto. Nenhuma outra camada deve referenciar um adapter de
/// InvoiSys.Infrastructure diretamente — sempre via a interface, resolvida aqui.
///
/// É também onde vive a política de resiliência (retry com backoff exponencial nos
/// erros transitórios) e o timeout de cada HttpClient: são preocupações de transporte,
/// não do adapter nem do domínio.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JiraOptions>(configuration.GetSection(JiraOptions.SecaoConfig));
        services.Configure<OpenRouterOptions>(
            configuration.GetSection(OpenRouterOptions.SecaoConfig));

        services.AddSingleton<PromptLoader>();

        AddJiraClient(services);
        AddLlmProvider(services);

        return services;
    }

    private static void AddJiraClient(IServiceCollection services)
    {
        services.AddHttpClient<JiraRestClient>((provider, http) =>
        {
            var opcoes = provider.GetRequiredService<IOptions<JiraOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(opcoes.BaseUrl))
            {
                http.BaseAddress = new Uri(opcoes.BaseUrl.TrimEnd('/'));
            }

            // Basic Auth com email + API token, conforme exige a Jira Cloud.
            var credencial = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{opcoes.Email}:{opcoes.ApiToken}"));
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credencial);
            http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            http.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(opcoes =>
        {
            // 3 tentativas no total (1 + 2 retries), backoff exponencial — mesmo
            // desenho do adapter Python. 429/5xx e falha de transporte são retentados;
            // 4xx de cliente (401, 404) propaga na primeira tentativa, porque tentar de
            // novo não mudaria o resultado.
            opcoes.Retry.MaxRetryAttempts = 2;
            opcoes.Retry.Delay = TimeSpan.FromSeconds(1);
            opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        });

        // Sem credencial configurada, cai para JiraClientPendente — sem BaseUrl o
        // HttpClient estouraria lá dentro e o usuário receberia um 500 opaco em vez de
        // "falta configurar o Jira".
        services.AddScoped<IJiraClient>(provider =>
        {
            var opcoes = provider.GetRequiredService<IOptions<JiraOptions>>().Value;
            return opcoes.EstaConfigurado
                ? provider.GetRequiredService<JiraRestClient>()
                : new JiraClientPendente();
        });
    }

    private static void AddLlmProvider(IServiceCollection services)
    {
        services.AddHttpClient<OpenRouterProvider>((provider, http) =>
        {
            var opcoes = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;

            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opcoes.ApiKey);

            // Geração de texto é lenta — timeout bem mais largo que o do Jira.
            http.Timeout = TimeSpan.FromSeconds(120);
        })
        .AddStandardResilienceHandler(opcoes =>
        {
            opcoes.Retry.MaxRetryAttempts = 2;
            opcoes.Retry.Delay = TimeSpan.FromSeconds(2);
            opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
            opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
        });

        // Sem chave configurada, cai para ProviderPendente — mantém o gate honesto de
        // 501 em vez de instanciar um adapter fadado a falhar em runtime.
        services.AddScoped<ILlmProvider>(provider =>
        {
            var opcoes = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            return opcoes.EstaConfigurado
                ? provider.GetRequiredService<OpenRouterProvider>()
                : new ProviderPendente();
        });
    }
}
