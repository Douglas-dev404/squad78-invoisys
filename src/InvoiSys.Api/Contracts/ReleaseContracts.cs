using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Contracts;

public sealed record HistoriaJiraOut(
    string Chave,
    string Titulo,
    string TipoIssue,
    bool PossuiReleaseNoteDedicada);

public sealed record ReleaseOut(
    string ChaveJira,
    string Status,
    int TotalHistorias,
    IReadOnlyList<HistoriaJiraOut> Historias);

public sealed record ItemComunicadoOut(
    string Categoria,
    string Texto,
    IReadOnlyList<string> Origens);

public sealed record ReleaseProcessadaOut(
    string ChaveJira,
    string Status,
    string? TituloExecutivo,
    string? ResumoExecutivo,
    IReadOnlyList<ItemComunicadoOut> Itens);
