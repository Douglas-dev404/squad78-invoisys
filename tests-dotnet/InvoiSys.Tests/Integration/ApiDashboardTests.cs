using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiDashboardTests
{
    private static WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.RemoveAll<IBloqueioProcessamento>();
                services.AddSingleton<IBloqueioProcessamento>(new FakeBloqueioProcessamento());
                services.ComUsuarios(AutenticacaoDeTeste.Revisor);
                services.AddScoped<IReleaseRepository>(_ => new FakeReleaseRepository());
            }));

    [Fact]
    public async Task Dashboard_sem_token_responde_401()
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/dashboard");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_autenticado_responde_200_no_contrato_da_issue()
    {
        using var app = CriarApp();
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.GetAsync("/api/v1/dashboard");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var totais = corpo.GetProperty("totais");
        totais.GetProperty("aprovadas").GetInt32().Should().Be(0);
        totais.GetProperty("aguardandoRevisao").GetInt32().Should().Be(0);
        totais.GetProperty("falharam").GetInt32().Should().Be(0);
        totais.GetProperty("exportacoes").GetInt32().Should().Be(0);
        corpo.GetProperty("ultimos7Dias").ValueKind.Should().Be(JsonValueKind.Array);
        corpo.GetProperty("recentes").GetArrayLength().Should().Be(0);
        var qualidade = corpo.GetProperty("qualidade");
        qualidade.GetProperty("itensGerados").GetInt32().Should().Be(0);
        qualidade.GetProperty("itensEditados").GetInt32().Should().Be(0);
        qualidade.GetProperty("itensExcluidos").GetInt32().Should().Be(0);
    }
}
