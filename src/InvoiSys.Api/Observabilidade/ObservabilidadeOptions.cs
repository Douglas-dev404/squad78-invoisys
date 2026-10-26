namespace InvoiSys.Api.Observabilidade;

public sealed class ObservabilidadeOptions
{
    public const string SecaoConfig = "Observabilidade";

    public bool Habilitada { get; init; }

    public string NomeServico { get; init; } = "invoisys-api";

    public LangfuseOptions Langfuse { get; init; } = new();

    public PrometheusOptions Prometheus { get; init; } = new();

    public sealed class LangfuseOptions
    {
        public string Endpoint { get; init; } = "";

        public string PublicKey { get; init; } = "";

        public string SecretKey { get; init; } = "";

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Endpoint)
            && !string.IsNullOrWhiteSpace(PublicKey)
            && !string.IsNullOrWhiteSpace(SecretKey);
    }

    public sealed class PrometheusOptions
    {
        public string Endpoint { get; init; } = "";

        public int IntervaloExportacaoSegundos { get; init; } = 15;

        public bool Configurado => !string.IsNullOrWhiteSpace(Endpoint);
    }
}
