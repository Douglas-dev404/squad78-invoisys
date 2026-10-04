namespace InvoiSys.Domain.Enums;

public enum StatusPipeline
{
    Pendente,
    Processando,
    AguardandoRevisao,
    Aprovado,
    Falhou,
}

public static class StatusPipelineExtensions
{
    private static readonly Dictionary<StatusPipeline, string> PorEnum = new()
    {
        [StatusPipeline.Pendente] = "pendente",
        [StatusPipeline.Processando] = "processando",
        [StatusPipeline.AguardandoRevisao] = "aguardando_revisao",
        [StatusPipeline.Aprovado] = "aprovado",
        [StatusPipeline.Falhou] = "falhou",
    };

    public static string ParaValor(this StatusPipeline status) => PorEnum[status];
}
