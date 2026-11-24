using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Integracoes;

/// <summary>
/// Configuração de Jira e IA em uso: variáveis de ambiente como base e o que o admin salvou na tela de
/// Integrações por cima. Singleton; <see cref="Recarregar"/> troca o retrato inteiro de uma vez só.
/// </summary>
public sealed class IntegracoesEmUso
{
    private readonly IConfiguration _configuracao;
    private readonly CofreSegredos _cofre;
    private readonly ILogger<IntegracoesEmUso> _logger;

    private volatile Retrato _atual;

    public IntegracoesEmUso(IConfiguration configuracao, CofreSegredos cofre, ILogger<IntegracoesEmUso> logger)
    {
        _configuracao = configuracao;
        _cofre = cofre;
        _logger = logger;
        _atual = Montar([]);
    }

    public JiraOptions Jira => _atual.Jira;

    public OpenAiOptions OpenAi => _atual.OpenAi;

    public OpenRouterOptions OpenRouter => _atual.OpenRouter;

    public ProvedorLlm Provedor => _atual.Provedor;

    public string ModeloAtivo => Provedor == ProvedorLlm.OpenAi ? OpenAi.Modelo : OpenRouter.Modelo;

    /// <summary>Chaves que hoje vêm do que foi salvo no sistema (o resto vem do ambiente ou está vazio).</summary>
    public IReadOnlySet<string> ChavesDoSistema => _atual.ChavesDoSistema;

    public void Recarregar(IEnumerable<ConfiguracaoIntegracao> linhas) => _atual = Montar(linhas);

    private Retrato Montar(IEnumerable<ConfiguracaoIntegracao> linhas)
    {
        var valores = new Dictionary<string, string>();
        foreach (var linha in linhas)
        {
            var valor = linha.Secreto ? _cofre.Decifrar(linha.Valor) : linha.Valor;
            if (valor is null)
            {
                _logger.LogWarning(
                    "Não foi possível decifrar a configuração {Chave}: a chave de criptografia mudou? "
                    + "Usando o valor do ambiente.",
                    linha.Chave);
                continue;
            }

            valores[linha.Chave] = valor;
        }

        var jira = _configuracao.GetSection(JiraOptions.SecaoConfig).Get<JiraOptions>() ?? new JiraOptions();
        Sobrescrever(valores, ChavesIntegracao.JiraBaseUrl, v => jira.BaseUrl = v);
        Sobrescrever(valores, ChavesIntegracao.JiraEmail, v => jira.Email = v);
        Sobrescrever(valores, ChavesIntegracao.JiraApiToken, v => jira.ApiToken = v);
        Sobrescrever(valores, ChavesIntegracao.JiraProjeto, v => jira.Projeto = v);

        var openAi = _configuracao.GetSection(OpenAiOptions.SecaoConfig).Get<OpenAiOptions>() ?? new OpenAiOptions();
        Sobrescrever(valores, ChavesIntegracao.OpenAiModelo, v => openAi.Modelo = v);
        Sobrescrever(valores, ChavesIntegracao.OpenAiApiKey, v => openAi.ApiKey = v);

        var openRouter = _configuracao.GetSection(OpenRouterOptions.SecaoConfig).Get<OpenRouterOptions>()
            ?? new OpenRouterOptions();
        Sobrescrever(valores, ChavesIntegracao.OpenRouterModelo, v => openRouter.Modelo = v);
        Sobrescrever(valores, ChavesIntegracao.OpenRouterApiKey, v => openRouter.ApiKey = v);

        var provedor = DependencyInjection.LerProvedorLlm(_configuracao);
        if (valores.TryGetValue(ChavesIntegracao.IaProvedor, out var escolhido)
            && Enum.TryParse<ProvedorLlm>(escolhido, ignoreCase: true, out var parseado)
            && Enum.IsDefined(parseado))
        {
            provedor = parseado;
        }

        return new Retrato(jira, openAi, openRouter, provedor, new HashSet<string>(valores.Keys));
    }

    private static void Sobrescrever(Dictionary<string, string> valores, string chave, Action<string> aplicar)
    {
        if (valores.TryGetValue(chave, out var valor) && !string.IsNullOrWhiteSpace(valor))
        {
            aplicar(valor.Trim());
        }
    }

    private sealed record Retrato(
        JiraOptions Jira,
        OpenAiOptions OpenAi,
        OpenRouterOptions OpenRouter,
        ProvedorLlm Provedor,
        IReadOnlySet<string> ChavesDoSistema);
}

/// <summary><c>IOptions&lt;T&gt;</c> que lê o retrato atual a cada acesso a <c>Value</c>.</summary>
public sealed class OpcoesAoVivo<T>(Func<T> origem) : IOptions<T>
    where T : class
{
    public T Value => origem();
}
