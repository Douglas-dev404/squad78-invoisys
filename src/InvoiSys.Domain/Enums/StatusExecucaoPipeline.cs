namespace InvoiSys.Domain.Enums;

/// <summary>
/// Status de uma execução individual do pipeline de IA sobre uma Release —
/// registro de rastreabilidade, não o status da Release em si
/// (ver <see cref="StatusPipeline"/>). Uma Release pode ter várias execuções ao
/// longo do tempo (reprocessamento manual, falha seguida de nova tentativa); cada
/// uma vira uma linha aqui, preservando histórico completo para auditoria.
/// </summary>
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
