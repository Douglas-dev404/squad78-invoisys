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
        ILlmProvider? llm = null,
        IReleaseRepository? repositorio = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IJiraClient>();
                services.RemoveAll<ILlmProvider>();
                services.RemoveAll<IReleaseRepository>();
                services.AddScoped(_ => jira ?? new FakeJiraClient());
                services.AddScoped(_ => llm ?? new FakeLlmProvider());
                // Testes de HTTP/roteamento/serialização não precisam de Postgres real —
                // isso já é coberto pelos testes de integração de ReleaseRepository e do
                // pipeline. Sem isso, o pipeline tentaria persistir contra a connection
                // string de produção, que não existe neste host de teste.
                services.AddScoped<IReleaseRepository>(_ => repositorio ?? new FakeReleaseRepository());
            }));

    private static HistoriaJira Historia(string chave, string? releaseNote = null) => new()
    {
        Chave = chave,
        Titulo = $"História {chave}",
        DescricaoTecnica = "Descrição técnica da implementação.",
        TipoIssue = "Story",
        TextoReleaseNote = releaseNote,
    };

    private static Release ReleaseComComunicado()
{
    var release = new Release(
        "RELEASE-2026-08",
        [Historia("INV-1")]);

    release.ConcluirProcessamento(
        [
            new ItemComunicado(
                Domain.Enums.CategoriaAlteracao.Correcao,
                "Texto do comunicado.",
                ["INV-1"])
        ],
        "Título da Release",
        "Resumo da Release");

    return release;
}

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
    public async Task Reprocessar_release_com_versao_aprovada_responde_409()
    {
        var repositorio = new FakeReleaseRepository();
        var aprovada = new Release("RELEASE-2026-08", [Historia("INV-1")]);
        aprovada.ConcluirProcessamento(
            [new ItemComunicado(Domain.Enums.CategoriaAlteracao.Correcao, "Texto aprovado.", ["INV-1"])],
            "Título",
            "Resumo");
        aprovada.Aprovar("revisora@invoisys.com", DateTimeOffset.UtcNow);
        await repositorio.SalvarAsync(aprovada);

        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("não chamar") };
        using var app = CriarApp(new FakeJiraClient { Historias = [Historia("INV-1")] }, llm, repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync("/api/v1/releases/RELEASE-2026-08/processar", null);

        resposta.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            "o estado do recurso impede a operação: é preciso reabrir a revisão antes");
        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.Should().Contain("Reabra a revisão", "a resposta diz ao usuário o que fazer");
        aprovada.Itens.Single().Texto.Should().Be("Texto aprovado.");
    }

    [Fact]
public async Task Aprovar_release_para_cliente_responde_204()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/aprovar",
        new
        {
            publico = "cliente",
            aprovadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.NoContent);

    release.VersaoCliente!.Status
        .Should()
        .Be(Domain.Enums.StatusRevisao.Aprovado);

    release.VersaoCliente.RevisadoPor
        .Should()
        .Be("victor");
}
    
    [Fact]
public async Task Aprovar_release_sem_comunicado_responde_409()
{
    var repositorio = new FakeReleaseRepository();

    var release = new Release(
        "RELEASE-2026-08",
        [Historia("INV-1")]);

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/aprovar",
        new
        {
            aprovadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.Conflict);

    var corpo = await resposta.Content.ReadAsStringAsync();

    corpo.Should().Contain("não tem comunicado gerado");
}

[Fact]
public async Task Reprovar_release_registra_motivo_e_responde_204()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/reprovar",
        new
        {
            publico = "cliente",
            motivo = "Texto precisa ser revisado.",
            revisadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.NoContent);

    release.VersaoCliente!.Status
        .Should()
        .Be(Domain.Enums.StatusRevisao.Reprovado);

    release.VersaoCliente.MotivoReprovacao
        .Should()
        .Be("Texto precisa ser revisado.");

    release.VersaoCliente.RevisadoPor
        .Should()
        .Be("victor");
}

[Fact]
public async Task Reprovar_release_sem_motivo_responde_422()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/reprovar",
        new
        {
            publico = "cliente",
            motivo = "",
            revisadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.UnprocessableEntity);
}

[Fact]
public async Task Reabrir_release_aprovada_responde_204()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    release.Aprovar(
        "victor",
        DateTimeOffset.UtcNow);

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/reabrir",
        new
        {
            publico = "cliente",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.NoContent);

    release.VersaoCliente!.Status
        .Should()
        .Be(Domain.Enums.StatusRevisao.AguardandoRevisao);

    release.VersaoCliente.RevisadoPor
        .Should()
        .BeNull();

    release.VersaoCliente.RevisadoEm
        .Should()
        .BeNull();
}

[Fact]
public async Task Reabrir_release_nao_aprovada_responde_422()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/reabrir",
        new
        {
            publico = "cliente",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.UnprocessableEntity);
}

[Fact]
public async Task Aprovar_release_inexistente_responde_404()
{
    using var app = CriarApp();
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-INEXISTENTE/aprovar",
        new
        {
            aprovadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.NotFound);
}

[Fact]
public async Task Aprovar_com_publico_invalido_responde_422()
{
    var repositorio = new FakeReleaseRepository();

    var release = ReleaseComComunicado();

    await repositorio.SalvarAsync(release);

    using var app = CriarApp(repositorio: repositorio);
    using var client = app.CreateClient();

    var resposta = await client.PostAsJsonAsync(
        "/api/v1/releases/RELEASE-2026-08/aprovar",
        new
        {
            publico = "financeiro",
            aprovadoPor = "victor",
        });

    resposta.StatusCode.Should()
        .Be(HttpStatusCode.UnprocessableEntity);
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
            FalhaAoBuscar = new JiraApiException(
                "Jira retornou 401."),
        };

        using var app = CriarApp(jira);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-2026-08/historias");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }
}
