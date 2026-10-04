namespace InvoiSys.Domain.Entities;

public sealed record HistoriaJira
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Chave { get; init; }

    public required string Titulo { get; init; }

    public required string DescricaoTecnica { get; init; }

    public required string TipoIssue { get; init; }

    public string? TextoReleaseNote { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public bool PossuiReleaseNoteDedicada => !string.IsNullOrWhiteSpace(TextoReleaseNote);

    public string TextoFonte => PossuiReleaseNoteDedicada ? TextoReleaseNote! : DescricaoTecnica;
}
