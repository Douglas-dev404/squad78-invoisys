using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface ITokenRedefinicaoSenhaRepository
{
    Task AdicionarAsync(TokenRedefinicaoSenha token, CancellationToken cancellationToken = default);

    Task<TokenRedefinicaoSenha?> BuscarPorHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<TokenRedefinicaoSenha?> BuscarUltimoDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<bool> ConsumirAsync(
        Guid id,
        DateTimeOffset agora,
        CancellationToken cancellationToken = default);

    Task InvalidarPendentesDoUsuarioAsync(
        Guid usuarioId,
        DateTimeOffset agora,
        CancellationToken cancellationToken = default);
}
