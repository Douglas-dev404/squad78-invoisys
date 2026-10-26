using System.Collections.Concurrent;
using FluentAssertions;
using InvoiSys.Api.Observabilidade;
using InvoiSys.Domain.Observabilidade;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace InvoiSys.Tests.Integration;

public class ObservabilidadeTests
{
    private sealed record Recebido(string Caminho, string? Autorizacao, string? VersaoIngestao);

    private static async Task<(WebApplication App, string Url, ConcurrentQueue<Recebido> Recebidos)> SubirColetorAsync()
    {
        var recebidos = new ConcurrentQueue<Recebido>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();

        app.MapPost("/{**caminho}", (HttpContext http) =>
        {
            recebidos.Enqueue(new Recebido(
                http.Request.Path,
                http.Request.Headers.Authorization.ToString(),
                http.Request.Headers["x-langfuse-ingestion-version"].ToString()));
            return Results.Ok();
        });

        await app.StartAsync();
        var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return (app, url, recebidos);
    }

    [Fact]
    public async Task Traces_vao_para_o_endpoint_do_Langfuse_e_metricas_para_o_do_Prometheus()
    {
        var (coletor, url, recebidos) = await SubirColetorAsync();
        await using var _ = coletor;

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Observabilidade:Habilitada"] = "true",
            ["Observabilidade:Langfuse:Endpoint"] = $"{url}/api/public/otel/v1/traces",
            ["Observabilidade:Langfuse:PublicKey"] = "pk-teste",
            ["Observabilidade:Langfuse:SecretKey"] = "sk-teste",
            ["Observabilidade:Prometheus:Endpoint"] = $"{url}/api/v1/otlp/v1/metrics",
        }).Build();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddObservabilidade(configuracao);
        await using var provedor = servicos.BuildServiceProvider();

        var tracer = provedor.GetRequiredService<TracerProvider>();
        var medidor = provedor.GetRequiredService<MeterProvider>();

        using (Telemetria.IniciarRaiz("teste.observabilidade"))
        {
        }

        Telemetria.PipelineBloqueios.Add(1);
        tracer.ForceFlush(10_000);
        medidor.ForceFlush(10_000);

        var traces = recebidos.Where(r => r.Caminho == "/api/public/otel/v1/traces").ToList();
        traces.Should().NotBeEmpty("o span raiz precisa chegar ao endpoint do Langfuse");
        traces.Should().OnlyContain(r => r.Autorizacao!.StartsWith("Basic ") && r.VersaoIngestao == "4");

        recebidos.Should().Contain(r => r.Caminho == "/api/v1/otlp/v1/metrics", "as métricas vão ao Prometheus");
        recebidos.Select(r => r.Caminho).Distinct()
            .Should().BeEquivalentTo(["/api/public/otel/v1/traces", "/api/v1/otlp/v1/metrics"]);
    }

    [Fact]
    public void Desligada_por_padrao_nao_registra_nenhum_provedor()
    {
        var servicos = new ServiceCollection();
        servicos.AddObservabilidade(new ConfigurationBuilder().Build());

        servicos.Should().NotContain(d => d.ServiceType == typeof(TracerProvider));
        servicos.Should().NotContain(d => d.ServiceType == typeof(MeterProvider));
    }
}
