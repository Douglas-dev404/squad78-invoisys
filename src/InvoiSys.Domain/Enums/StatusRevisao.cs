namespace InvoiSys.Domain.Enums;

/// <summary>
/// Status de revisão humana de uma <see cref="Entities.VersaoComunicado"/>.
///
/// Deliberadamente separado de <see cref="StatusPipeline"/>: aquele descreve o estado
/// da <b>geração</b> (a IA rodou?), este descreve o estado da <b>revisão</b> (o humano
/// aprovou?). Misturar os dois em um enum só foi o que travou o desenho anterior —
/// uma Release podia estar com o pipeline concluído e, ao mesmo tempo, ter a versão
/// Cliente aprovada e a versão Suporte ainda em ajuste. Um campo só não representa isso.
/// </summary>
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
