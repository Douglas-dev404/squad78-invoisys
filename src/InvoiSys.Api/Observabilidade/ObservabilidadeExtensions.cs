using System.Text;
using InvoiSys.Domain.Observabilidade;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InvoiSys.Api.Observabilidade;

public static class ObservabilidadeExtensions
{
    private static readonly string[] MetersDoRuntime =
    [
        "System.Runtime",
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Server.Kestrel",
        "Microsoft.AspNetCore.Routing",
        "System.Net.Http",
        "Npgsql",
    ];

    public static IServiceCollection AddObservabilidade(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opcoes = configuration.GetSection(ObservabilidadeOptions.SecaoConfig)
            .Get<ObservabilidadeOptions>() ?? new ObservabilidadeOptions();

        if (!opcoes.Habilitada)
        {
            return services;
        }

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso.AddService(opcoes.NomeServico));

        if (opcoes.Langfuse.Configurado)
        {
            otel.WithTracing(tracing => tracing
                .AddSource(Telemetria.Nome)
                .AddOtlpExporter(exportador =>
                {
                    exportador.Endpoint = new Uri(opcoes.Langfuse.Endpoint);
                    exportador.Protocol = OtlpExportProtocol.HttpProtobuf;
                    exportador.Headers = CabecalhosLangfuse(opcoes.Langfuse);
                }));
        }

        if (opcoes.Prometheus.Configurado)
        {
            otel.WithMetrics(metricas => metricas
                .AddMeter(Telemetria.Nome)
                .AddMeter(MetersDoRuntime)
                .AddOtlpExporter((exportador, leitor) =>
                {
                    exportador.Endpoint = new Uri(opcoes.Prometheus.Endpoint);
                    exportador.Protocol = OtlpExportProtocol.HttpProtobuf;
                    leitor.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds =
                        opcoes.Prometheus.IntervaloExportacaoSegundos * 1000;
                }));
        }

        return services;
    }

    private static string CabecalhosLangfuse(ObservabilidadeOptions.LangfuseOptions langfuse)
    {
        var credencial = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{langfuse.PublicKey}:{langfuse.SecretKey}"));

        // Sem a versão 4 o Langfuse leva até 10 minutos para mostrar o trace.
        return $"Authorization=Basic {credencial},x-langfuse-ingestion-version=4";
    }
}
