using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

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
