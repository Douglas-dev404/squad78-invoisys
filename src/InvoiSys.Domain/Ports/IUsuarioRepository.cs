using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
