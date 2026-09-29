using InvoiSys.Domain.Entities;
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

public sealed record ReleaseResumoOut(string ChaveJira, string Status);

/// <summary>
/// Diferente de <see cref="ItemComunicadoOut"/> (retorno enxuto de
/// POST /processar, sempre recém-gerado e nunca revisado ainda): este DTO alimenta a
/// tela de revisão, então expõe o estado de revisão em si — se o item foi excluído, e
/// por quê. <c>Texto</c> aqui é <see cref="ItemComunicado.TextoFinal"/>
/// (edição humana sobrescreve o texto da IA quando existir), não o texto cru gerado.
/// </summary>
public sealed record ItemComunicadoDetalheOut(
    string Categoria,
    string Texto,
    IReadOnlyList<string> Origens,
    bool Incluido,
    string? MotivoExclusao);

public sealed record VersaoComunicadoOut(
    string Publico,
    string Status,
    string? TituloExecutivo,
    string? ResumoExecutivo,
    IReadOnlyList<ItemComunicadoDetalheOut> Itens,
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
