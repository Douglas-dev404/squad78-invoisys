using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class DestinatarioRepository(InvoiSysDbContext contexto) : IDestinatarioRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<Destinatario?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.Destinatarios.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Destinatario>> BuscarPorIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await _contexto.Destinatarios
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToListAsync(cancellationToken);

    public async Task<bool> ExisteEmailNoPublicoAsync(
        string email,
        PublicoAlvo publico,
        Guid? ignorarId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return await _contexto.Destinatarios.AnyAsync(
            d => d.Email == normalizado && d.Publico == publico && d.Id != ignorarId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Destinatario>> ListarAsync(
        PublicoAlvo? publico,
        bool incluirInativos,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Destinatarios.AsNoTracking();

        if (publico is not null)
        {
            consulta = consulta.Where(d => d.Publico == publico);
        }

        if (!incluirInativos)
        {
            consulta = consulta.Where(d => d.Ativo);
        }

        return await consulta
            .OrderBy(d => d.Nome)
            .ThenBy(d => d.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task SalvarAsync(Destinatario destinatario, CancellationToken cancellationToken = default)
    {
        if (_contexto.Entry(destinatario).State == EntityState.Detached)
        {
            _contexto.Destinatarios.Add(destinatario);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
