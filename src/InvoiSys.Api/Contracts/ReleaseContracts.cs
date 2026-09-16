using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Contracts;

/// <summary>
/// DTOs da API — nunca expor entidades de domínio direto na resposta HTTP. Isso mantém
/// o contrato de API estável mesmo que o domínio mude internamente.
///
/// Categoria e status saem como string no formato herdado do contrato original
/// (nova_funcionalidade, aguardando_revisao), não como o nome PascalCase do membro nem
/// como número — quem consome a API não deve reparar que trocamos de linguagem.
/// </summary>
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
