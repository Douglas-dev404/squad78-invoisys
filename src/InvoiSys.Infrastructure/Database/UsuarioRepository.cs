using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class UsuarioRepository(InvoiSysDbContext contexto) : IUsuarioRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<Usuario?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<Usuario?> BuscarPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        if (_contexto.Entry(usuario).State == EntityState.Detached)
        {
            _contexto.Usuarios.Add(usuario);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
