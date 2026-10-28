using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiValidacaoTests
{
    private static WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.AddScoped<IReleaseRepository>(_ => new FakeReleaseRepository());
            }));

    [Theory]
    [InlineData("aprovar", "{}", "publico")]
    [InlineData("aprovar", "{\"publico\":\"xpto\"}", "publico")]
    [InlineData("reprovar", "{\"publico\":\"cliente\"}", "motivo")]
    [InlineData("reprovar", "{\"publico\":\"cliente\",\"motivo\":\"   \"}", "motivo")]
    [InlineData("reprovar", "{\"publico\":\"xpto\",\"motivo\":\"Revisar\"}", "publico")]
    [InlineData("reabrir", "{}", "publico")]
    [InlineData("reabrir", "{\"publico\":\"xpto\"}", "publico")]
    [InlineData("editar", "{}", "texto")]
    [InlineData("editar", "{\"texto\":\"   \"}", "texto")]
    public async Task Entrada_invalida_retorna_422_com_errors_antes_de_consultar_release(
        string acao, string json, string campo)
    {
        using var app = CriarApp();
        using var client = app.CreateClient();
        var rota = acao == "editar"
            ? "/api/v1/releases/INEXISTENTE/itens/10000000-0000-0000-0000-000000000001"
            : $"/api/v1/releases/INEXISTENTE/{acao}";
        using var request = new HttpRequestMessage(acao == "editar" ? HttpMethod.Patch : HttpMethod.Post, rota)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        var resposta = await client.SendAsync(request);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        corpo.RootElement.GetProperty("status").GetInt32().Should().Be(422);
        corpo.RootElement.GetProperty("title").GetString().Should().Be("Dados inválidos");
        corpo.RootElement.GetProperty("detail").GetString().Should().Be("Um ou mais campos são inválidos.");
        var erros = corpo.RootElement.GetProperty("errors");
        erros.EnumerateObject().Select(erro => erro.Name).Should().Equal(campo);
        erros.GetProperty(campo).EnumerateArray().Select(erro => erro.GetString())
            .Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Reprovar_devolve_todos_os_campos_invalidos_em_uma_resposta()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.PostAsync("/api/v1/releases/INEXISTENTE/reprovar",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        corpo.RootElement.GetProperty("errors").EnumerateObject().Select(erro => erro.Name)
            .Should().BeEquivalentTo("publico", "motivo");
    }
}
