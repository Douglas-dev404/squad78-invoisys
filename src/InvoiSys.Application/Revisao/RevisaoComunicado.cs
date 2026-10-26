using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Revisao;

public sealed record ItemRevisado(ItemComunicado Item, PublicoAlvo Publico);

public sealed class RevisorInvalidoException()
    : Exception("O usuário do token não existe ou está desativado.");

public sealed class RevisaoComunicado(IReleaseRepository releases, IUsuarioRepository usuarios)
{
    public Task AprovarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor) => release.Aprovar(revisor, DateTimeOffset.UtcNow, publico),
            cancellationToken);

    public Task ReprovarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        Guid revisorId,
        string motivo,
        CancellationToken cancellationToken = default) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor) => release.Reprovar(revisor, motivo, DateTimeOffset.UtcNow, publico),
            cancellationToken);

    public Task ReabrirAsync(
        string chaveRelease,
        PublicoAlvo publico,
        CancellationToken cancellationToken = default) =>
        RevisarAsync(
            chaveRelease,
            release => release.Reabrir(publico),
            cancellationToken);

    public Task<ItemRevisado> EditarItemAsync(
        string chaveRelease,
        Guid itemId,
        string texto,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            release => release.EditarItem(itemId, texto),
            cancellationToken);

    public Task<ItemRevisado> ExcluirItemAsync(
        string chaveRelease,
        Guid itemId,
        string? motivo,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            release => release.ExcluirItem(itemId, motivo),
            cancellationToken);

    public Task<ItemRevisado> ReincluirItemAsync(
        string chaveRelease,
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            release => release.ReincluirItem(itemId),
            cancellationToken);

    private async Task RevisarComoAsync(
        string chaveRelease,
        Guid revisorId,
        Action<Release, Usuario> revisao,
        CancellationToken cancellationToken)
    {
        var revisor = await usuarios.BuscarPorIdAsync(revisorId, cancellationToken);
        if (revisor is null || !revisor.Ativo)
        {
            throw new RevisorInvalidoException();
        }

        await RevisarAsync(chaveRelease, release => revisao(release, revisor), cancellationToken);
    }

    private Task<ItemRevisado> RevisarItemAsync(
        string chaveRelease,
        Guid itemId,
        Func<Release, ItemComunicado> revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            chaveRelease,
            release => new ItemRevisado(revisao(release), release.PublicoDoItem(itemId)),
            cancellationToken);

    private Task RevisarAsync(
        string chaveRelease,
        Action<Release> revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            chaveRelease,
            release =>
            {
                revisao(release);
                return true;
            },
            cancellationToken);

    private async Task<T> RevisarAsync<T>(
        string chaveRelease,
        Func<Release, T> revisao,
        CancellationToken cancellationToken)
    {
        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        var resultado = revisao(release);

        await releases.SalvarAsync(release, cancellationToken);

        return resultado;
    }
}
