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

    private static readonly Dictionary<string, StatusPipeline> PorValor =
        PorEnum.ToDictionary(par => par.Value, par => par.Key);

    public static string ParaValor(this StatusPipeline status) => PorEnum[status];

    public static bool TentarConverter(string? valor, out StatusPipeline status)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            status = default;
            return false;
        }

        return PorValor.TryGetValue(valor.Trim().ToLowerInvariant(), out status);
    }
}
