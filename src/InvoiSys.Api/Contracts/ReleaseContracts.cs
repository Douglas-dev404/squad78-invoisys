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

public sealed record ReleaseResumoOut(string ChaveJira, string Status, DateTimeOffset CriadoEm);

public sealed record VersaoComunicadoOut(
    string Publico,
    string Status,
    string? TituloExecutivo,
    string? ResumoExecutivo,
    IReadOnlyList<ItemRevisaoOut> Itens,
    string? RevisadoPor,
    DateTimeOffset? RevisadoEm,
    string? MotivoReprovacao);

public sealed record ExecucaoPipelineOut(
    string Status,
    string? ModeloLlm,
    string? Erro,
    DateTimeOffset IniciadoEm,
    DateTimeOffset? ConcluidoEm);

public sealed record ReleaseDetalheOut(
    string ChaveJira,
    string Status,
    IReadOnlyList<HistoriaJiraOut> Historias,
    IReadOnlyList<VersaoComunicadoOut> Versoes,
    IReadOnlyList<ExecucaoPipelineOut> Execucoes);
