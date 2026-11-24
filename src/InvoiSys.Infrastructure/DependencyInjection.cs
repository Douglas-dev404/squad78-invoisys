using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Infrastructure.Email;
using InvoiSys.Infrastructure.Integracoes;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Infrastructure.Llm;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        AddIntegracoesEditaveis(services, configuration);

        AddAuth(services, configuration);

        AddJiraClient(services);
        AddLlmProvider(services, configuration);
        AddDatabase(services, configuration);

        return services;
    }

    // Jira e IA podem ser editados na tela de Integrações: IOptions<T> passa a ler o retrato em uso a cada acesso.
    private static void AddIntegracoesEditaveis(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new CofreSegredos(configuration));
        services.AddSingleton(provider => new IntegracoesEmUso(
            configuration,
            provider.GetRequiredService<CofreSegredos>(),
            (ILogger<IntegracoesEmUso>?)provider.GetService<ILogger<IntegracoesEmUso>>()
                ?? NullLogger<IntegracoesEmUso>.Instance));
        services.AddSingleton<IMonitorExecucao, MonitorExecucaoEmMemoria>();
        services.AddSingleton<IModeloIaAtivo, ModeloIaAtivo>();
        services.AddScoped<AdministracaoIntegracoes>();

        services.AddSingleton<IOptions<JiraOptions>>(
            p => new OpcoesAoVivo<JiraOptions>(() => p.GetRequiredService<IntegracoesEmUso>().Jira));
        services.AddSingleton<IOptions<OpenAiOptions>>(
            p => new OpcoesAoVivo<OpenAiOptions>(() => p.GetRequiredService<IntegracoesEmUso>().OpenAi));
        services.AddSingleton<IOptions<OpenRouterOptions>>(
            p => new OpcoesAoVivo<OpenRouterOptions>(() => p.GetRequiredService<IntegracoesEmUso>().OpenRouter));
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
        services.AddScoped<IConfiguracaoIntegracaoRepository, ConfiguracaoIntegracaoRepository>();

        // Trava por Release (ADR-031): advisory lock de sessão numa conexão própria, fora do DbContext,
        // para durar a execução inteira do pipeline sem depender de transação.
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddScoped<IBloqueioProcessamento, PostgresBloqueioProcessamento>();
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
        LerProvedorLlm(configuration); // falha no boot se Llm:Provedor for inválido

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

        services.AddScoped<ILlmProvider>(provider => provider.GetRequiredService<IntegracoesEmUso>().Provedor switch
        {
            ProvedorLlm.OpenAi => provider.GetRequiredService<IOptions<OpenAiOptions>>().Value.EstaConfigurado
                ? provider.GetRequiredService<OpenAiProvider>()
                : new ProviderPendente("OpenAi:ApiKey"),
            _ => provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value.EstaConfigurado
                ? provider.GetRequiredService<OpenRouterProvider>()
                : new ProviderPendente(),
        });
    }

    internal static ProvedorLlm LerProvedorLlm(IConfiguration configuration)
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
