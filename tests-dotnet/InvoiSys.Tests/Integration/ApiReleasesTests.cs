using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Llm;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes da API ponta a ponta pelo pipeline HTTP real do ASP.NET — roteamento, DI,
/// serialização — com as portas externas (Jira, LLM) substituídas por fakes. Nenhuma
/// chamada de rede, nenhuma credencial.
/// </summary>
public class ApiReleasesTests
{
    private static WebApplicationFactory<Program> CriarApp(
        IJiraClient? jira = null,
        ILlmProvider? llm = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IJiraClient>();
                services.RemoveAll<ILlmProvider>();
                services.AddScoped(_ => jira ?? new FakeJiraClient());
                services.AddScoped(_ => llm ?? new FakeLlmProvider());
            }));

    private static HistoriaJira Historia(string chave, string? releaseNote = null) => new()
    {
        Chave = chave,
        Titulo = $"História {chave}",
        DescricaoTecnica = "Descrição técnica da implementação.",
        TipoIssue = "Story",
        TextoReleaseNote = releaseNote,
    };

    [Fact]
    public async Task Health_responde_ok()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/health");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task Buscar_historias_devolve_a_lista_do_Jira_sem_rodar_a_IA()
    {
        var jira = new FakeJiraClient
        {
            Historias = [Historia("INV-1"), Historia("INV-2", releaseNote: "Texto pro cliente.")],
        };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("não chamar") };

        using var app = CriarApp(jira, llm);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-2026-08/historias");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        corpo.GetProperty("chaveJira").GetString().Should().Be("RELEASE-2026-08");
        corpo.GetProperty("status").GetString().Should().Be("pendente");
        corpo.GetProperty("totalHistorias").GetInt32().Should().Be(2);

        var historias = corpo.GetProperty("historias").EnumerateArray().ToList();
        historias[0].GetProperty("possuiReleaseNoteDedicada").GetBoolean().Should().BeFalse();
        historias[1].GetProperty("possuiReleaseNoteDedicada").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Processar_release_devolve_itens_em_aguardando_revisao()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider
        {
            CategoriaFixa = Domain.Enums.CategoriaAlteracao.Correcao,
            TextoReescrito = "Corrigimos o cálculo do imposto retido.",
            TituloEResumo = ("Release de agosto", "Resumo executivo."),
        };

        using var app = CriarApp(jira, llm);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync("/api/v1/releases/RELEASE-2026-08/processar", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        corpo.GetProperty("status").GetString().Should().Be(
            "aguardando_revisao",
            "o resultado nunca sai publicado direto");
        corpo.GetProperty("tituloExecutivo").GetString().Should().Be("Release de agosto");

        var itens = corpo.GetProperty("itens").EnumerateArray().ToList();
        itens.Should().ContainSingle();
        itens[0].GetProperty("categoria").GetString().Should().Be("correcao");
        itens[0].GetProperty("texto").GetString().Should()
            .Be("Corrigimos o cálculo do imposto retido.");
        itens[0].GetProperty("origens").EnumerateArray().Select(o => o.GetString())
            .Should().Equal("INV-1");
    }

    [Fact]
    public async Task Sem_provider_de_LLM_configurado_a_API_responde_501()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };

        using var app = CriarApp(jira, new ProviderPendente());
        using var client = app.CreateClient();

        var resposta = await client.PostAsync("/api/v1/releases/RELEASE-2026-08/processar", null);

        resposta.StatusCode.Should().Be(
            HttpStatusCode.NotImplemented,
            "funcionalidade não configurada é 501, não 500 nem resposta falsa");
    }

    [Fact]
    public async Task Resposta_invalida_do_LLM_vira_502_com_corpo_e_nao_500_mudo()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider
        {
            FalhaAoChamar = new LlmRespostaInvalidaException("modelo devolveu JSON inválido"),
        };

        using var app = CriarApp(jira, llm);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync("/api/v1/releases/RELEASE-2026-08/processar", null);

        resposta.StatusCode.Should().Be(
            HttpStatusCode.BadGateway,
            "falha da dependência de IA não é erro interno nosso");

        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.Should().Contain("modelo devolveu JSON inválido", "a causa precisa chegar a quem chamou");
    }

    [Fact]
    public async Task Sem_credencial_de_Jira_configurada_a_API_responde_501_e_nao_500()
    {
        // Regressão: sem BaseUrl, o HttpClient estourava InvalidOperationException lá
        // no fundo do adapter e o usuário recebia 500 com stack trace, sem pista de que
        // faltava configuração.
        using var app = CriarApp(new Infrastructure.Jira.JiraClientPendente());
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-2026-08/historias");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task Erro_do_Jira_vira_502_e_nao_500()
    {
        var jira = new FakeJiraClient
        {
            FalhaAoBuscar = new Infrastructure.Jira.JiraApiException(
                "Jira retornou 401", HttpStatusCode.Unauthorized),
        };

        using var app = CriarApp(jira);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-2026-08/historias");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }
}
