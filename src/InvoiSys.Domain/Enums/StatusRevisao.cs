namespace InvoiSys.Domain.Enums;

public enum StatusRevisao
{
    AguardandoRevisao,
    Aprovado,
    Reprovado,
}

public static class StatusRevisaoExtensions
{
    private static readonly Dictionary<StatusRevisao, string> PorEnum = new()
    {
        [StatusRevisao.AguardandoRevisao] = "aguardando_revisao",
        [StatusRevisao.Aprovado] = "aprovado",
        [StatusRevisao.Reprovado] = "reprovado",
    };

    public static string ParaValor(this StatusRevisao status) => PorEnum[status];
}
