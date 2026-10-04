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
/// Ver ADR-007 em docs/decisoes-arquiteturais.md — revisão humana é invariante
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

    private static readonly Dictionary<string, StatusPipeline> PorValor =
        PorEnum.ToDictionary(par => par.Value, par => par.Key);

    public static string ParaValor(this StatusPipeline status) => PorEnum[status];

    /// <summary>
    /// Converte o valor textual de volta para o enum. Case-insensitive porque a
    /// origem é query string de API (ex.: <c>?status=</c>), não entrada controlada.
    /// </summary>
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
