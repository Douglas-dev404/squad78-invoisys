using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using InvoiSys.Tests.Unit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiHistoricoTests
{
    private const string Chave = "RELEASE-2026-11";

    private sealed record Ambiente(
        WebApplicationFactory<Program> App,
        FakeReleaseRepository Releases,
        Usuario Ana);

    private static Ambiente CriarApp()
    {
        var releases = new FakeReleaseRepository();
        var ana = AutenticacaoDeTeste.UmRevisor("Ana Revisora");
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.AddRange([AutenticacaoDeTeste.Revisor, ana]);
        var historico = new FakeHistoricoRevisaoRepository(releases, usuarios);

        var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<IHistoricoRevisaoRepository>();
                services.RemoveAll<IJiraClient>();
                services.RemoveAll<ILlmProvider>();
                services.AddSingleton<IReleaseRepository>(releases);
                services.AddSingleton<IUsuarioRepository>(usuarios);
                services.AddSingleton<IHistoricoRevisaoRepository>(historico);
                services.AddScoped<IJiraClient>(_ => new FakeJiraClient
                {
                    Historias =
                    [
                        new HistoriaJira
                        {
                            Chave = "INV-1",
                            Titulo = "Título",
                            DescricaoTecnica = "Descrição técnica.",
                            TipoIssue = "Story",
                        },
                    ],
                });
                services.AddScoped<ILlmProvider>(_ => new FakeLlmProvider());
            }));

        return new Ambiente(app, releases, ana);
    }

    private static async Task<Release> ReleaseProcessada(FakeReleaseRepository releases)
    {
        var release = new Release(
            Chave,
            [new HistoriaJira { Chave = "INV-1", Titulo = "Título", DescricaoTecnica = "D", TipoIssue = "Story" }]);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Correcao, "Texto da IA", ["INV-1"])],
            "Título",
            "Resumo");
        await releases.SalvarAsync(release);
        return release;
    }

    private static async Task<JsonElement> LerHistorico(HttpClient client)
    {
        var resposta = await client.GetAsync($"/api/v1/releases/{Chave}/historico");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var a = CriarApp();
        using var client = a.App.CreateClient();

        (await client.GetAsync($"/api/v1/releases/{Chave}/historico")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Release_inexistente_responde_404()
    {
        var a = CriarApp();
        using var client = a.App.CriarClienteAutenticado();

        (await client.GetAsync("/api/v1/releases/NAO-EXISTE/historico")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Release_sem_eventos_responde_200_com_lista_vazia()
    {
        var a = CriarApp();
        await a.Releases.SalvarAsync(new Release(Chave, []));
        using var client = a.App.CriarClienteAutenticado();

        var historico = await LerHistorico(client);

        historico.ValueKind.Should().Be(JsonValueKind.Array);
        historico.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Processar_registra_o_usuario_do_token_no_historico()
    {
        var a = CriarApp();
        using var client = a.App.CriarClienteAutenticado(a.Ana);

        (await client.PostAsync($"/api/v1/releases/{Chave}/processar", null)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var historico = await LerHistorico(client);
        historico.GetArrayLength().Should().Be(1);
        historico[0].GetProperty("acao").GetString().Should().Be("processar");
        historico[0].GetProperty("usuario").GetProperty("nome").GetString().Should().Be("Ana Revisora");
        historico[0].GetProperty("usuario").GetProperty("id").GetGuid().Should().Be(a.Ana.Id);
        historico[0].GetProperty("detalhe").GetProperty("totalItens").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Linha_do_tempo_mostra_aprovar_reabrir_aprovar_e_a_edicao_de_item()
    {
        var a = CriarApp();
        var release = await ReleaseProcessada(a.Releases);
        var itemId = release.VersaoCliente!.Itens[0].Id;
        using var victor = a.App.CriarClienteAutenticado();
        using var ana = a.App.CriarClienteAutenticado(a.Ana);

        (await victor.PostAsJsonAsync($"/api/v1/releases/{Chave}/aprovar", new { publico = "cliente" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ana.PostAsJsonAsync($"/api/v1/releases/{Chave}/reabrir", new { publico = "cliente" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ana.PatchAsJsonAsync($"/api/v1/releases/{Chave}/itens/{itemId}", new { texto = "Texto revisado" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await ana.PostAsJsonAsync($"/api/v1/releases/{Chave}/aprovar", new { publico = "cliente" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var historico = await LerHistorico(victor);

        historico.EnumerateArray().Select(e => e.GetProperty("acao").GetString()).Should().Equal(
            "processar", "aprovar", "reabrir", "editar_item", "aprovar");
        historico.EnumerateArray().Select(e => e.GetProperty("usuario").GetProperty("nome").GetString())
            .Should().Equal("Victor Revisor", "Victor Revisor", "Ana Revisora", "Ana Revisora", "Ana Revisora");
        historico.EnumerateArray().Select(e => e.GetProperty("publico").GetString())
            .Should().OnlyContain(p => p == "cliente");

        var edicao = historico[3];
        edicao.GetProperty("itemId").GetGuid().Should().Be(itemId);
        edicao.GetProperty("detalhe").GetProperty("textoAnterior").GetString().Should().Be("Texto da IA");
        edicao.GetProperty("detalhe").GetProperty("textoNovo").GetString().Should().Be("Texto revisado");
        historico[1].GetProperty("itemId").ValueKind.Should().Be(JsonValueKind.Null);
        historico[1].GetProperty("detalhe").ValueKind.Should().Be(JsonValueKind.Null);
        historico.EnumerateArray().Select(e => e.GetProperty("ocorridoEm").GetDateTimeOffset())
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Reprovar_e_excluir_item_registram_motivo_no_detalhe()
    {
        var a = CriarApp();
        var release = await ReleaseProcessada(a.Releases);
        var itemId = release.VersaoCliente!.Itens[0].Id;
        using var client = a.App.CriarClienteAutenticado(a.Ana);

        await client.PostAsJsonAsync($"/api/v1/releases/{Chave}/itens/{itemId}/excluir", new { motivo = "Interno" });
        await client.PostAsJsonAsync($"/api/v1/releases/{Chave}/itens/{itemId}/reincluir", new { });
        await client.PostAsJsonAsync(
            $"/api/v1/releases/{Chave}/reprovar",
            new { publico = "cliente", motivo = "Tom técnico demais" });

        var historico = await LerHistorico(client);

        historico.EnumerateArray().Select(e => e.GetProperty("acao").GetString()).Should().Equal(
            "processar", "excluir_item", "reincluir_item", "reprovar");
        historico[1].GetProperty("detalhe").GetProperty("motivo").GetString().Should().Be("Interno");
        historico[3].GetProperty("detalhe").GetProperty("motivo").GetString().Should().Be("Tom técnico demais");
    }
}
