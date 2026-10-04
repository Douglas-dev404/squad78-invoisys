using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure;

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
        AddDatabase(services, configuration);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default não configurada. Defina via appsettings, " +
                "variável de ambiente ConnectionStrings__Default, ou .env (Docker Compose).");

        services.AddDbContext<InvoiSysDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IHistoriaJiraRepository, HistoriaJiraRepository>();
        services.AddScoped<IExecucaoPipelineRepository, ExecucaoPipelineRepository>();
        services.AddScoped<IComunicadoExportadoRepository, ComunicadoExportadoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IDestaqueHeroRepository, DestaqueHeroRepository>();
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
            opcoes.Retry.MaxRetryAttempts = 2;
            opcoes.Retry.Delay = TimeSpan.FromSeconds(1);
            opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        });

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

        services.AddScoped<ILlmProvider>(provider =>
        {
            var opcoes = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            return opcoes.EstaConfigurado
                ? provider.GetRequiredService<OpenRouterProvider>()
                : new ProviderPendente();
        });
    }
}
