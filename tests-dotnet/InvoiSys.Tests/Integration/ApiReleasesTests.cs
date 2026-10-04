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
                // Sem Postgres aqui: o pipeline persistiria contra uma connection string que não existe neste host.
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

    [Fact]
    public async Task Listar_releases_sem_filtro_devolve_todas_as_persistidas()
    {
        var repositorio = new FakeReleaseRepository();
        await repositorio.SalvarAsync(new Release("RELEASE-2026-08", [Historia("INV-1")]));
        var processando = new Release("RELEASE-2026-09", [Historia("INV-2")]);
        processando.MarcarProcessando();
        await repositorio.SalvarAsync(processando);

        using var app = CriarApp(repositorio: repositorio);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.EnumerateArray().Select(r => r.GetProperty("chaveJira").GetString())
            .Should().BeEquivalentTo(["RELEASE-2026-08", "RELEASE-2026-09"]);
    }

    [Fact]
    public async Task Listar_releases_com_filtro_de_status_so_devolve_o_status_pedido()
    {
        var repositorio = new FakeReleaseRepository();
        await repositorio.SalvarAsync(new Release("RELEASE-2026-08", [Historia("INV-1")]));
        var processando = new Release("RELEASE-2026-09", [Historia("INV-2")]);
        processando.MarcarProcessando();
        await repositorio.SalvarAsync(processando);

        using var app = CriarApp(repositorio: repositorio);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases?status=processando");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var releases = corpo.EnumerateArray().ToList();
        releases.Should().ContainSingle();
        releases[0].GetProperty("chaveJira").GetString().Should().Be("RELEASE-2026-09");
    }

    [Fact]
    public async Task Listar_releases_com_status_invalido_responde_422()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases?status=nao-existe");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.Should().Contain("nao-existe", "a resposta diz qual valor foi rejeitado");
    }

    [Fact]
    public async Task Buscar_release_existente_devolve_todas_as_versoes_por_publico()
    {
        var repositorio = new FakeReleaseRepository();
        var release = new Release("RELEASE-2026-08", [Historia("INV-1")]);
        release.ConcluirProcessamento(
            [new ItemComunicado(Domain.Enums.CategoriaAlteracao.Melhoria, "Pro cliente.", ["INV-1"])],
            "Título Cliente",
            "Resumo Cliente");
        release.ConcluirProcessamento(
            [new ItemComunicado(Domain.Enums.CategoriaAlteracao.Melhoria, "Pro suporte.", ["INV-1"])],
            "Título Suporte",
            "Resumo Suporte",
            Domain.Enums.PublicoAlvo.Suporte);
        await repositorio.SalvarAsync(release);

        using var app = CriarApp(repositorio: repositorio);
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-2026-08");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        corpo.GetProperty("chaveJira").GetString().Should().Be("RELEASE-2026-08");
        corpo.GetProperty("historias").GetArrayLength().Should().Be(1);

        var versoes = corpo.GetProperty("versoes").EnumerateArray().ToList();
        versoes.Select(v => v.GetProperty("publico").GetString())
            .Should().BeEquivalentTo(["cliente", "suporte"], "não é só o atalho da versão Cliente");

        var cliente = versoes.Single(v => v.GetProperty("publico").GetString() == "cliente");
        var itemCliente = cliente.GetProperty("itens").EnumerateArray().Single();
        itemCliente.GetProperty("id").GetGuid().Should().Be(release.VersaoCliente!.Itens[0].Id);
        itemCliente.GetProperty("publico").GetString().Should().Be("cliente");
        itemCliente.GetProperty("texto").GetString().Should().Be("Pro cliente.");
        itemCliente.GetProperty("textoFinal").GetString().Should().Be("Pro cliente.");
        itemCliente.GetProperty("incluido").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Id_do_item_no_detalhe_e_o_alvo_das_rotas_de_item()
    {
        var repositorio = new FakeReleaseRepository();
        var release = new Release("RELEASE-2026-08", [Historia("INV-1")]);
        release.ConcluirProcessamento(
            [new ItemComunicado(Domain.Enums.CategoriaAlteracao.Melhoria, "Texto da IA.", ["INV-1"])],
            "Título",
            "Resumo");
        await repositorio.SalvarAsync(release);

        using var app = CriarApp(repositorio: repositorio);
        using var client = app.CreateClient();

        var detalhe = await client.GetFromJsonAsync<JsonElement>("/api/v1/releases/RELEASE-2026-08");
        var itemId = detalhe.GetProperty("versoes")[0].GetProperty("itens")[0].GetProperty("id").GetGuid();

        var edicao = await client.PatchAsJsonAsync(
            $"/api/v1/releases/RELEASE-2026-08/itens/{itemId}",
            new { texto = "Texto revisado." });
        edicao.StatusCode.Should().Be(HttpStatusCode.OK);

        var depois = await client.GetFromJsonAsync<JsonElement>("/api/v1/releases/RELEASE-2026-08");
        var item = depois.GetProperty("versoes")[0].GetProperty("itens")[0];
        item.GetProperty("texto").GetString().Should().Be("Texto da IA.", "o texto da IA é preservado");
        item.GetProperty("textoEditadoManualmente").GetString().Should().Be("Texto revisado.");
        item.GetProperty("textoFinal").GetString().Should().Be("Texto revisado.");
    }

    [Fact]
    public async Task Buscar_release_inexistente_responde_404()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/releases/RELEASE-INEXISTENTE");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
