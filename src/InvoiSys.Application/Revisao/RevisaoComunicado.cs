using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Revisao;

public sealed record ItemRevisado(ItemComunicado Item, PublicoAlvo Publico);

public sealed class RevisaoComunicado(IReleaseRepository releases)
{
    public Task AprovarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        string aprovadoPor,
        CancellationToken cancellationToken = default) =>
        RevisarAsync(
            chaveRelease,
            release => release.Aprovar(aprovadoPor, DateTimeOffset.UtcNow, publico),
            cancellationToken);

    public Task ReprovarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        string revisadoPor,
        string motivo,
        CancellationToken cancellationToken = default) =>
        RevisarAsync(
            chaveRelease,
            release => release.Reprovar(revisadoPor, motivo, DateTimeOffset.UtcNow, publico),
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
