using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>
/// Registro de rastreabilidade de uma execução do pipeline de IA sobre uma Release.
///
/// Existe separado de <see cref="Release"/> porque uma Release pode ser reprocessada
/// mais de uma vez (falha seguida de nova tentativa, ajuste manual seguido de
/// reprocessamento) — sem isso, cada nova tentativa apagaria o rastro da anterior,
/// que é exatamente o log/observabilidade que o MVP pede.
/// </summary>
public sealed class ExecucaoPipeline
{
    public ExecucaoPipeline(Guid releaseId, string? modeloLlm)
    {
        Id = Guid.NewGuid();
        ReleaseId = releaseId;
        ModeloLlm = modeloLlm;
        Status = StatusExecucaoPipeline.Iniciada;
        IniciadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public Guid ReleaseId { get; private init; }

    public StatusExecucaoPipeline Status { get; private set; }

    /// <summary>Modelo do OpenRouter usado nesta execução (ex: "openai/gpt-4o-mini").</summary>
    public string? ModeloLlm { get; private init; }

    public string? Erro { get; private set; }

    public DateTimeOffset IniciadoEm { get; private init; }

    public DateTimeOffset? ConcluidoEm { get; private set; }

    public void MarcarConcluida()
    {
        Status = StatusExecucaoPipeline.Concluida;
        ConcluidoEm = DateTimeOffset.UtcNow;
    }

    public void MarcarFalha(string erro)
    {
        Status = StatusExecucaoPipeline.Falhou;
        Erro = erro;
        ConcluidoEm = DateTimeOffset.UtcNow;
    }
}
