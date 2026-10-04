using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiSys.Tests.Integration;

public class OpenApiTests
{
    private static WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"));

    private static async Task<JsonDocument> BuscarSpecAsync()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/openapi/v1.json");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_UI_e_servido_em_development_lendo_o_spec_gerado()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var pagina = await client.GetAsync("/swagger/index.html");
        pagina.StatusCode.Should().Be(HttpStatusCode.OK);

        var configuracao = await client.GetStringAsync("/swagger/index.js");
        configuracao.Should().Contain("/openapi/v1.json");
    }

    [Fact]
    public async Task Swagger_UI_e_spec_nao_ficam_expostos_fora_de_development()
    {
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = app.CreateClient();

        (await client.GetAsync("/swagger/index.html")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Spec_publica_todas_as_rotas_da_API()
    {
        using var spec = await BuscarSpecAsync();

        var caminhos = spec.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToList();

        caminhos.Should().BeEquivalentTo([
            "/health",
            "/api/v1/releases/{chaveRelease}/aprovar",
            "/api/v1/releases/{chaveRelease}/reprovar",
            "/api/v1/releases/{chaveRelease}/reabrir",
            "/api/v1/releases/{chaveRelease}/historias",
            "/api/v1/releases/{chaveRelease}/processar",
            "/api/v1/releases/{chaveRelease}/itens/{itemId}",
            "/api/v1/releases/{chaveRelease}/itens/{itemId}/excluir",
            "/api/v1/releases/{chaveRelease}/itens/{itemId}/reincluir"
        ]);
    }

    [Fact]
    public async Task Cada_rota_descreve_o_formato_da_resposta_de_sucesso()
    {
        using var spec = await BuscarSpecAsync();

        var processar = spec.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/releases/{chaveRelease}/processar")
            .GetProperty("post");

        var ok = processar.GetProperty("responses").GetProperty("200");

        ok.TryGetProperty("content", out var content).Should().BeTrue(
            "a resposta de sucesso precisa declarar o corpo que devolve");

        content.GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString()
            .Should().Contain("ReleaseProcessadaOut");
    }

    [Fact]
    public async Task Schemas_dos_DTOs_sao_publicados_com_seus_campos()
    {
        using var spec = await BuscarSpecAsync();

        var schemas = spec.RootElement.GetProperty("components").GetProperty("schemas");

        var nomes = schemas.EnumerateObject().Select(s => s.Name).ToList();
        nomes.Should().Contain(["ReleaseOut", "ReleaseProcessadaOut", "ItemComunicadoOut"]);

        var campos = schemas.GetProperty("ItemComunicadoOut")
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToList();

        campos.Should().BeEquivalentTo("categoria", "texto", "origens");
    }

    [Fact]
    public async Task Respostas_de_erro_documentadas_incluem_o_gate_de_nao_configurado()
    {
        using var spec = await BuscarSpecAsync();

        var respostas = spec.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/releases/{chaveRelease}/processar")
            .GetProperty("post")
            .GetProperty("responses");

        respostas.TryGetProperty("501", out _).Should().BeTrue();
        respostas.TryGetProperty("502", out _).Should().BeTrue();

        respostas.TryGetProperty("409", out _).Should().BeTrue();
    }
}
