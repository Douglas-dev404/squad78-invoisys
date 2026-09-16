namespace InvoiSys.Domain.Entities;

/// <summary>
/// Representa uma história (issue) do Jira, tal como veio da origem — sem qualquer
/// processamento de IA ainda. É o dado bruto de entrada do pipeline.
///
/// Imutável (record com init) pelo mesmo motivo do dataclass(frozen=True) original:
/// o estágio 1 do pipeline produz uma cópia limpa, nunca muta a original.
/// </summary>
public sealed record HistoriaJira
{
    /// <summary>Ex: "INV-1234".</summary>
    public required string Chave { get; init; }

    public required string Titulo { get; init; }

    public required string DescricaoTecnica { get; init; }

    /// <summary>Story, Bug, Task, etc — vocabulário do Jira, não nosso.</summary>
    public required string TipoIssue { get; init; }

    /// <summary>Conteúdo da subtarefa "Release Note", quando existir.</summary>
    public string? TextoReleaseNote { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    /// <summary>
    /// Regra de negócio: se a subtarefa Release Note existe e tem conteúdo, ela é a
    /// fonte preferencial de texto — a IA deve priorizá-la sobre a descrição técnica
    /// crua. Ver documentação interna de Regras de Negócio.
    /// </summary>
    public bool PossuiReleaseNoteDedicada => !string.IsNullOrWhiteSpace(TextoReleaseNote);

    /// <summary>
    /// Texto que efetivamente alimenta o pipeline de IA: Release Note dedicada quando
    /// existe, senão a descrição técnica.
    /// </summary>
    public string TextoFonte => PossuiReleaseNoteDedicada ? TextoReleaseNote! : DescricaoTecnica;
}
