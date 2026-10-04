using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiItensTests
{
    private const string Chave = "RELEASE-2026-08";

    private static WebApplicationFactory<Program> CriarApp(IReleaseRepository repositorio) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.AddScoped(_ => repositorio);
            }));

    private static async Task<(FakeReleaseRepository Repositorio, Release Release)> ReleaseProcessadaAsync()
    {
        var release = new Release(
            Chave,
            [new HistoriaJira { Chave = "INV-1", Titulo = "Título", DescricaoTecnica = "Descrição", TipoIssue = "Story" }]);
        release.ConcluirProcessamento(
            [
                new ItemComunicado(CategoriaAlteracao.Correcao, "Texto da IA", ["INV-1"]),
                new ItemComunicado(CategoriaAlteracao.Outros, "Item interno", ["INV-1"]),
            ],
            "Título",
            "Resumo");

        var repositorio = new FakeReleaseRepository();
        await repositorio.SalvarAsync(release);
        return (repositorio, release);
    }

    [Fact]
    public async Task Editar_item_devolve_o_item_com_o_texto_revisado()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        var item = release.VersaoCliente!.Itens[0];
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PatchAsJsonAsync(
            $"/api/v1/releases/{Chave}/itens/{item.Id}",
            new { texto = "Corrigimos o cálculo do ICMS-ST." });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("id").GetGuid().Should().Be(item.Id);
        corpo.GetProperty("publico").GetString().Should().Be("cliente");
        corpo.GetProperty("categoria").GetString().Should().Be("correcao");
        corpo.GetProperty("texto").GetString().Should().Be("Texto da IA");
        corpo.GetProperty("textoEditadoManualmente").GetString().Should().Be("Corrigimos o cálculo do ICMS-ST.");
        corpo.GetProperty("textoFinal").GetString().Should().Be("Corrigimos o cálculo do ICMS-ST.");

        var salva = await repositorio.BuscarPorChaveJiraAsync(Chave);
        salva!.VersaoCliente!.Itens[0].TextoFinal.Should().Be("Corrigimos o cálculo do ICMS-ST.");
    }

    [Fact]
    public async Task Editar_item_sem_texto_responde_422_e_nao_muda()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();
        var item = release.VersaoCliente!.Itens[0];

        var resposta = await client.PatchAsJsonAsync(
            $"/api/v1/releases/{Chave}/itens/{item.Id}",
            new { texto = "  " });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        item.TextoEditadoManualmente.Should().BeNull();
    }

    [Fact]
    public async Task Excluir_item_com_motivo_tira_o_item_do_comunicado()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        var item = release.VersaoCliente!.Itens[1];
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            $"/api/v1/releases/{Chave}/itens/{item.Id}/excluir",
            new { motivo = "Alteração interna, não interessa ao cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("incluido").GetBoolean().Should().BeFalse();
        corpo.GetProperty("motivoExclusao").GetString().Should().Be("Alteração interna, não interessa ao cliente");
        release.VersaoCliente!.ItensPublicaveis.Should().ContainSingle();
    }

    [Fact]
    public async Task Excluir_item_sem_corpo_tambem_funciona()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        var item = release.VersaoCliente!.Itens[1];
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync($"/api/v1/releases/{Chave}/itens/{item.Id}/excluir", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK, "o motivo da exclusão é opcional");
        item.Incluido.Should().BeFalse();
        item.MotivoExclusao.Should().BeNull();
    }

    [Fact]
    public async Task Reincluir_item_devolve_o_item_ao_comunicado()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        var item = release.ExcluirItem(release.VersaoCliente!.Itens[1].Id, "Engano");
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync($"/api/v1/releases/{Chave}/itens/{item.Id}/reincluir", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("incluido").GetBoolean().Should().BeTrue();
        corpo.GetProperty("motivoExclusao").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Item_de_versao_aprovada_responde_409_e_nao_muda()
    {
        var (repositorio, release) = await ReleaseProcessadaAsync();
        var item = release.VersaoCliente!.Itens[0];
        release.Aprovar("revisora@invoisys.com", DateTimeOffset.UtcNow);
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var editar = await client.PatchAsJsonAsync(
            $"/api/v1/releases/{Chave}/itens/{item.Id}",
            new { texto = "Texto que ninguém aprovou" });
        var excluir = await client.PostAsync($"/api/v1/releases/{Chave}/itens/{item.Id}/excluir", null);

        editar.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await editar.Content.ReadAsStringAsync()).Should().Contain("reabra a revisão", "a resposta diz o que fazer");
        excluir.StatusCode.Should().Be(HttpStatusCode.Conflict);
        item.TextoFinal.Should().Be("Texto da IA");
        item.Incluido.Should().BeTrue();
    }

    [Fact]
    public async Task Release_inexistente_responde_404()
    {
        using var app = CriarApp(new FakeReleaseRepository());
        using var client = app.CreateClient();

        var resposta = await client.PostAsync($"/api/v1/releases/NAO-EXISTE/itens/{Guid.NewGuid()}/reincluir", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Item_de_outra_release_responde_404()
    {
        var (repositorio, _) = await ReleaseProcessadaAsync();
        var outra = new Release(
            "RELEASE-2026-09",
            [new HistoriaJira { Chave = "INV-9", Titulo = "Título", DescricaoTecnica = "Descrição", TipoIssue = "Story" }]);
        outra.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto de outra Release", ["INV-9"])],
            "Título",
            "Resumo");
        await repositorio.SalvarAsync(outra);
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PatchAsJsonAsync(
            $"/api/v1/releases/{Chave}/itens/{outra.VersaoCliente!.Itens[0].Id}",
            new { texto = "Tentativa pela Release errada" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        outra.VersaoCliente!.Itens[0].TextoEditadoManualmente.Should().BeNull();
    }

    [Fact]
    public async Task ItemId_que_nao_e_uuid_responde_404()
    {
        var (repositorio, _) = await ReleaseProcessadaAsync();
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        var resposta = await client.PostAsync($"/api/v1/releases/{Chave}/itens/abc/reincluir", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound, "a rota só casa com itemId no formato uuid");
    }
}
