using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// A documentação da API é entregável do projeto, então é testada como código.
///
/// O modo de falhar aqui é silencioso: um handler que devolve IResult sem declarar
/// .Produces&lt;T&gt;() continua funcionando perfeitamente, mas o spec sai com "200 OK"
/// e nenhum schema — e ninguém percebe até alguém tentar consumir a API.
/// </summary>
public class OpenApiTests
{
    private static WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            // O spec só é publicado em Development (ver Program.cs).
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
    public async Task Spec_publica_todas_as_rotas_da_API()
    {
        using var spec = await BuscarSpecAsync();

        var caminhos = spec.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToList();

        caminhos.Should().BeEquivalentTo(
            "/health",
            "/api/v1/releases/{chaveRelease}/historias",
            "/api/v1/releases/{chaveRelease}/processar");
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

        // Um "200 OK" sem content é exatamente o sintoma da regressão.
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

        // 501 (falta configurar Jira/LLM) e 502 (dependência externa falhou) são
        // respostas previsíveis desta API, não imprevistos: quem consome precisa vê-las.
        respostas.TryGetProperty("501", out _).Should().BeTrue();
        respostas.TryGetProperty("502", out _).Should().BeTrue();
    }
}
