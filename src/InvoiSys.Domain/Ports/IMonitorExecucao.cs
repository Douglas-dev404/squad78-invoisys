namespace InvoiSys.Domain.Ports;

public static class TipoEventoExecucao
{
    public const string PipelineIniciado = "pipeline_iniciado";
    public const string Etapa = "etapa";
    public const string IaChamada = "ia_chamada";
    public const string IaResposta = "ia_resposta";
    public const string IaErro = "ia_erro";
    public const string PipelineConcluido = "pipeline_concluido";
    public const string PipelineFalhou = "pipeline_falhou";
}

public sealed record EventoExecucao(
    long Id,
    DateTimeOffset Em,
    string? Release,
    string Tipo,
    string Mensagem,
    string? Prompt = null,
    string? Resposta = null,
    string? Modelo = null,
    long? DuracaoMs = null);

/// <summary>
/// Canal de eventos da execução do pipeline de IA, para a tela "Execução da IA" ao vivo.
/// Só guarda em memória os últimos eventos: não é auditoria, é observação.
/// </summary>
public interface IMonitorExecucao
{
    /// <summary>Marca a Release em andamento para os eventos publicados dentro deste fluxo assíncrono.</summary>
    IDisposable IniciarEscopo(string chaveRelease);

    void Publicar(
        string tipo,
        string mensagem,
        string? prompt = null,
        string? resposta = null,
        string? modelo = null,
        long? duracaoMs = null);

    IReadOnlyList<EventoExecucao> Depois(long ultimoId, string? release = null);
}
