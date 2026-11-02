using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public sealed record EventoRevisaoResumo(
    Guid Id,
    AcaoRevisao Acao,
    PublicoAlvo Publico,
    Guid? ItemId,
    Guid UsuarioId,
    string UsuarioNome,
    DateTimeOffset OcorridoEm,
    IReadOnlyDictionary<string, object?>? Detalhe);

public interface IHistoricoRevisaoRepository
{
    Task<IReadOnlyList<EventoRevisaoResumo>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);
}
