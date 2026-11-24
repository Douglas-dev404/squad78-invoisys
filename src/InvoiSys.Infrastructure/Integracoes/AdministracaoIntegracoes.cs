using System.Diagnostics;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;

namespace InvoiSys.Infrastructure.Integracoes;

public sealed record IntegracaoJira(
    string BaseUrl, string Email, string Projeto, bool TokenConfigurado, bool TokenDoSistema, bool Configurado);

public sealed record IntegracaoProvedorIa(string Modelo, bool ChaveConfigurada, bool ChaveDoSistema);

public sealed record IntegracaoIa(
    string Provedor, IntegracaoProvedorIa OpenAi, IntegracaoProvedorIa OpenRouter, bool Configurado);

public sealed record IntegracoesVisao(IntegracaoJira Jira, IntegracaoIa Ia);

public sealed record ResultadoTeste(bool Ok, string Mensagem, long DuracaoMs);

/// <summary>
/// Lê e grava as integrações (Jira e IA) editadas na tela de Integrações. Segredos entram cifrados no banco
/// e nunca saem daqui: a leitura só informa se existem.
/// </summary>
public sealed class AdministracaoIntegracoes(
    IConfiguracaoIntegracaoRepository repositorio,
    CofreSegredos cofre,
    IntegracoesEmUso emUso,
    IJiraClient jira,
    ILlmProvider llm,
    IMonitorExecucao monitor)
{
    public IntegracoesVisao Obter()
    {
        var doSistema = emUso.ChavesDoSistema;
        var jiraOpcoes = emUso.Jira;

        return new IntegracoesVisao(
            new IntegracaoJira(
                jiraOpcoes.BaseUrl,
                jiraOpcoes.Email,
                jiraOpcoes.ProjetoEfetivo,
                !string.IsNullOrWhiteSpace(jiraOpcoes.ApiToken),
                doSistema.Contains(ChavesIntegracao.JiraApiToken),
                jiraOpcoes.EstaConfigurado),
            new IntegracaoIa(
                emUso.Provedor.ToString(),
                new IntegracaoProvedorIa(
                    emUso.OpenAi.Modelo,
                    emUso.OpenAi.EstaConfigurado,
                    doSistema.Contains(ChavesIntegracao.OpenAiApiKey)),
                new IntegracaoProvedorIa(
                    emUso.OpenRouter.Modelo,
                    emUso.OpenRouter.EstaConfigurado,
                    doSistema.Contains(ChavesIntegracao.OpenRouterApiKey)),
                emUso.Provedor == ProvedorLlm.OpenAi ? emUso.OpenAi.EstaConfigurado : emUso.OpenRouter.EstaConfigurado));
    }

    /// <summary>Carrega no retrato em uso o que está salvo no banco (chamado no boot).</summary>
    public async Task CarregarAsync(CancellationToken cancellationToken = default) =>
        emUso.Recarregar(await repositorio.ListarAsync(cancellationToken));

    public async Task SalvarJiraAsync(
        string? baseUrl, string? email, string? projeto, string? apiToken,
        CancellationToken cancellationToken = default)
    {
        var gravar = new List<ConfiguracaoIntegracao>();
        Adicionar(gravar, ChavesIntegracao.JiraBaseUrl, baseUrl?.Trim().TrimEnd('/'));
        Adicionar(gravar, ChavesIntegracao.JiraEmail, email?.Trim());
        Adicionar(gravar, ChavesIntegracao.JiraProjeto, projeto?.Trim().ToUpperInvariant());
        Adicionar(gravar, ChavesIntegracao.JiraApiToken, apiToken);
        await GravarAsync(gravar, cancellationToken);
    }

    public async Task SalvarIaAsync(
        string provedor, string? modelo, string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var openAi = string.Equals(provedor, nameof(ProvedorLlm.OpenAi), StringComparison.OrdinalIgnoreCase);
        var gravar = new List<ConfiguracaoIntegracao>();
        Adicionar(gravar, ChavesIntegracao.IaProvedor, openAi ? nameof(ProvedorLlm.OpenAi) : nameof(ProvedorLlm.OpenRouter));
        Adicionar(gravar, openAi ? ChavesIntegracao.OpenAiModelo : ChavesIntegracao.OpenRouterModelo, modelo?.Trim());
        Adicionar(gravar, openAi ? ChavesIntegracao.OpenAiApiKey : ChavesIntegracao.OpenRouterApiKey, apiKey);
        await GravarAsync(gravar, cancellationToken);
    }

    public async Task<ResultadoTeste> TestarJiraAsync(CancellationToken cancellationToken = default)
    {
        var relogio = Stopwatch.StartNew();
        try
        {
            var versoes = await jira.ListarVersoesAsync(cancellationToken);
            return new ResultadoTeste(true, $"Conectado. {versoes.Count} versão(ões) no projeto.", relogio.ElapsedMilliseconds);
        }
        catch (Exception exc) when (exc is IntegracaoExternaException or ProviderNaoConfiguradoException
            or HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            return new ResultadoTeste(false, Resumir(exc), relogio.ElapsedMilliseconds);
        }
    }

    public async Task<ResultadoTeste> TestarIaAsync(CancellationToken cancellationToken = default)
    {
        var relogio = Stopwatch.StartNew();
        using var escopo = monitor.IniciarEscopo("teste de conexão");
        try
        {
            monitor.Publicar(TipoEventoExecucao.Etapa, "Testando a conexão com a IA.", modelo: emUso.ModeloAtivo);
            var categoria = await llm.CategorizarAsync(
                "Corrigido o cálculo do ICMS-ST na emissão de NF-e.", cancellationToken);
            monitor.Publicar(TipoEventoExecucao.PipelineConcluido, "Teste de conexão concluído: a IA respondeu.", modelo: emUso.ModeloAtivo);
            return new ResultadoTeste(true, $"Conectado. A IA respondeu (categoria: {categoria.ParaValor()}).", relogio.ElapsedMilliseconds);
        }
        catch (Exception exc) when (exc is IntegracaoExternaException or ProviderNaoConfiguradoException
            or HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            monitor.Publicar(TipoEventoExecucao.PipelineFalhou, $"Teste de conexão falhou: {Resumir(exc)}", modelo: emUso.ModeloAtivo);
            return new ResultadoTeste(false, Resumir(exc), relogio.ElapsedMilliseconds);
        }
    }

    // Em branco = manter o que já existe (é assim que a tela deixa os segredos intocados).
    private void Adicionar(List<ConfiguracaoIntegracao> gravar, string chave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return;
        }

        var secreto = ChavesIntegracao.Secretas.Contains(chave);
        gravar.Add(new ConfiguracaoIntegracao(chave, secreto ? cofre.Cifrar(valor.Trim()) : valor, secreto));
    }

    private async Task GravarAsync(List<ConfiguracaoIntegracao> gravar, CancellationToken cancellationToken)
    {
        await repositorio.SalvarAsync(gravar, [], cancellationToken);
        emUso.Recarregar(await repositorio.ListarAsync(cancellationToken));
    }

    private static string Resumir(Exception exc)
    {
        var mensagem = exc.Message.ReplaceLineEndings(" ");
        return mensagem.Length > 240 ? mensagem[..240] + "…" : mensagem;
    }
}
