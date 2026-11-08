using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class TokenRedefinicaoSenhaRepository(InvoiSysDbContext contexto)
    : ITokenRedefinicaoSenhaRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task AdicionarAsync(
        TokenRedefinicaoSenha token,
        CancellationToken cancellationToken = default)
    {
        _contexto.TokensRedefinicaoSenha.Add(token);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<TokenRedefinicaoSenha?> BuscarPorHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await _contexto.TokensRedefinicaoSenha
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<TokenRedefinicaoSenha?> BuscarUltimoDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default) =>
        await _contexto.TokensRedefinicaoSenha
            .AsNoTracking()
            .Where(t => t.UsuarioId == usuarioId)
            .OrderByDescending(t => t.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ConsumirAsync(
        Guid id,
        DateTimeOffset agora,
        CancellationToken cancellationToken = default) =>
        await _contexto.TokensRedefinicaoSenha
            .Where(t => t.Id == id && t.UsadoEm == null && t.ExpiraEm > agora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEm, agora), cancellationToken) == 1;

    public async Task InvalidarPendentesDoUsuarioAsync(
        Guid usuarioId,
        DateTimeOffset agora,
        CancellationToken cancellationToken = default) =>
        await _contexto.TokensRedefinicaoSenha
            .Where(t => t.UsuarioId == usuarioId && t.UsadoEm == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEm, agora), cancellationToken);
}
