using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly Dictionary<Guid, Usuario> _porId = [];

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return Task.FromResult(_porId.Values.FirstOrDefault(u => u.Email == normalizado));
    }

    public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_porId.Count > 0);

    public Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _porId[usuario.Id] = usuario;
        return Task.CompletedTask;
    }
}