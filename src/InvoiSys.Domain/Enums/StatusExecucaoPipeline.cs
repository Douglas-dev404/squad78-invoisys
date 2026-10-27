namespace InvoiSys.Domain.Enums;

public enum StatusExecucaoPipeline
{
    Iniciada,
    Concluida,
    Falhou,
}

public static class StatusExecucaoPipelineExtensions
{
    private static readonly Dictionary<StatusExecucaoPipeline, string> PorEnum = new()
    {
        [StatusExecucaoPipeline.Iniciada] = "iniciada",
        [StatusExecucaoPipeline.Concluida] = "concluida",
        [StatusExecucaoPipeline.Falhou] = "falhou",
    };

    public static string ParaValor(this StatusExecucaoPipeline status) => PorEnum[status];
}
