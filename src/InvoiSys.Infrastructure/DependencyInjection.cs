using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Infrastructure.Email;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Infrastructure.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SecaoConfig));

        services.AddSingleton<PromptLoader>();

        AddAuth(services, configuration);

        AddJiraClient(services);
        AddLlmProvider(services, configuration);
        AddDatabase(services, configuration);

        return services;
    }

    private static void AddAuth(IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(AuthOptions.SecaoConfig).Get<AuthOptions>() ?? new AuthOptions();
        auth.Validar();

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SecaoConfig));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHashSenha, Argon2idHashSenha>();
        services.AddSingleton<IEmissorToken, JwtEmissorToken>();
        services.AddScoped<AdminInicialSemeador>();
        services.AddScoped<IEnvioEmail, EnvioEmailPendente>();
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
        services.AddScoped<ITokenRedefinicaoSenhaRepository, TokenRedefinicaoSenhaRepository>();
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

    private static void AddLlmProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provedor = LerProvedorLlm(configuration);

        services.AddHttpClient<OpenRouterProvider>((provider, http) =>
        {
            var opcoes = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            ConfigurarClienteLlm(http, opcoes.ApiKey);
        })
        .AddStandardResilienceHandler(ConfigurarResilienciaLlm);

        services.AddHttpClient<OpenAiProvider>((provider, http) =>
        {
            var opcoes = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            ConfigurarClienteLlm(http, opcoes.ApiKey);
        })
        .AddStandardResilienceHandler(ConfigurarResilienciaLlm);

        services.AddScoped<ILlmProvider>(provider => provedor switch
        {
            ProvedorLlm.OpenAi => provider.GetRequiredService<IOptions<OpenAiOptions>>().Value.EstaConfigurado
                ? provider.GetRequiredService<OpenAiProvider>()
                : new ProviderPendente("OpenAi:ApiKey"),
            _ => provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value.EstaConfigurado
                ? provider.GetRequiredService<OpenRouterProvider>()
                : new ProviderPendente(),
        });
    }

    private static ProvedorLlm LerProvedorLlm(IConfiguration configuration)
    {
        var valor = configuration["Llm:Provedor"];
        if (string.IsNullOrWhiteSpace(valor))
        {
            return ProvedorLlm.OpenRouter;
        }

        return Enum.TryParse<ProvedorLlm>(valor.Trim(), ignoreCase: true, out var provedor)
            && Enum.IsDefined(provedor)
            ? provedor
            : throw new InvalidOperationException(
                $"Llm:Provedor inválido: '{valor}'. Use OpenRouter ou OpenAi (variável Llm__Provedor).");
    }

    private static void ConfigurarClienteLlm(HttpClient http, string apiKey)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        http.Timeout = TimeSpan.FromSeconds(120);
    }

    private static void ConfigurarResilienciaLlm(HttpStandardResilienceOptions opcoes)
    {
        opcoes.Retry.MaxRetryAttempts = 2;
        opcoes.Retry.Delay = TimeSpan.FromSeconds(2);
        opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
        opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
        opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
    }
}
