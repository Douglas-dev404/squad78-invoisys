using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace InvoiSys.Domain.Observabilidade;

public static class Telemetria
{
    public const string Nome = "InvoiSys";

    public static readonly ActivitySource Atividades = new(Nome);

    private static readonly Meter Medidor = new(Nome);

    // Activity.Current = null: parentContext default do .NET significa "use Activity.Current", não "sem pai"; sem isso o span herda o "não amostrado" da Activity da requisição HTTP e some.
    public static Activity? IniciarRaiz(string nome)
    {
        Activity.Current = null;
        return Atividades.StartActivity(nome, ActivityKind.Internal);
    }

    public static readonly Counter<long> PipelineExecucoes = Medidor.CreateCounter<long>(
        "invoisys.pipeline.execucoes",
        description: "Execuções do pipeline de geração, por resultado.");

    public static readonly Histogram<double> PipelineDuracao = Medidor.CreateHistogram<double>(
        "invoisys.pipeline.duracao",
        unit: "s",
        description: "Duração do pipeline de geração de ponta a ponta.",
        advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [1, 2.5, 5, 10, 20, 30, 60, 120, 300] });

    public static readonly Counter<long> PipelineBloqueios = Medidor.CreateCounter<long>(
        "invoisys.pipeline.bloqueios",
        description: "Execuções barradas porque a mesma Release já estava em processamento.");

    public static readonly Counter<long> LlmChamadas = Medidor.CreateCounter<long>(
        "invoisys.llm.chamadas",
        description: "Chamadas ao LLM, por estágio, modelo e resultado.");

    public static readonly Histogram<double> LlmDuracao = Medidor.CreateHistogram<double>(
        "invoisys.llm.duracao",
        unit: "s",
        description: "Latência de cada chamada ao LLM.",
        advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.1, 0.25, 0.5, 1, 2, 5, 10, 20, 30, 60] });

    public static readonly Counter<long> LlmTokens = Medidor.CreateCounter<long>(
        "invoisys.llm.tokens",
        description: "Tokens consumidos no LLM, por tipo (entrada/saida) e modelo.");
}
