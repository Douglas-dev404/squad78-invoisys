using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IUsuarioRepository"/> via EF Core — mesmo desenho de
/// <see cref="ReleaseRepository"/>: consultas rastreadas (o usuário volta pronto para
/// <see cref="Usuario.Desativar"/> etc.) e <see cref="SalvarAsync"/> decidindo Add/Update
/// pelo estado de rastreamento.
///
/// E-mail comparado exatamente como gravado: normalizar caixa é regra de negócio da
/// autenticação (e teria que valer também no cadastro), não algo para esta camada
/// decidir sozinha.
/// </summary>
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
