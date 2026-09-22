namespace InvoiSys.Domain.Enums;

/// <summary>
/// Status do pipeline de geração de uma Release Note.
///
/// O pipeline tem 5 estágios de IA (extração, categorização, agrupamento, reescrita,
/// título/resumo). Não modelamos um status por estágio — isso é detalhe de execução
/// do serviço de orquestração, não estado persistente relevante pro domínio. O que o
/// domínio precisa saber é: a geração ainda não rodou, está rodando, terminou e
/// aguarda revisão humana, ou já foi aprovada e publicada.
///
/// Ver documentação interna de Regras de Negócio — revisão humana é invariante
/// obrigatório, não opcional.
/// </summary>
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
