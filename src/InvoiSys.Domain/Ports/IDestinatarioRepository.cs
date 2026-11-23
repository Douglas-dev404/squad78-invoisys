using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface IDestinatarioRepository
{
    Task<Destinatario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Destinatario>> BuscarPorIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteEmailNoPublicoAsync(
        string email,
        PublicoAlvo publico,
        Guid? ignorarId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Destinatario>> ListarAsync(
        PublicoAlvo? publico,
        bool incluirInativos,
        CancellationToken cancellationToken = default);

    Task SalvarAsync(Destinatario destinatario, CancellationToken cancellationToken = default);
}
