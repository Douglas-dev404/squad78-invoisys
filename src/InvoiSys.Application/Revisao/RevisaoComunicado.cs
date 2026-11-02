using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Revisao;

public sealed record ItemRevisado(ItemComunicado Item, PublicoAlvo Publico);

public sealed class RevisorInvalidoException()
    : Exception("O usuário do token não existe ou está desativado.");

public static class UsuarioResponsavel
{
    public static async Task<Usuario> ObterAtivoAsync(
        IUsuarioRepository usuarios,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, cancellationToken);
        return usuario is { Ativo: true } ? usuario : throw new RevisorInvalidoException();
    }
}

public sealed class RevisaoComunicado(
    IReleaseRepository releases,
    IUsuarioRepository usuarios,
    TimeProvider relogio)
{
    public Task AprovarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor, agora) => release.Aprovar(revisor, agora, publico),
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
            (release, revisor, agora) => release.Reprovar(revisor, motivo, agora, publico),
            cancellationToken);

    public Task ReabrirAsync(
        string chaveRelease,
        PublicoAlvo publico,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor, agora) => release.Reabrir(revisor, agora, publico),
            cancellationToken);

    public Task<ItemRevisado> EditarItemAsync(
        string chaveRelease,
        Guid itemId,
        string texto,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            revisorId,
            (release, revisor, agora) => release.EditarItem(itemId, texto, revisor, agora),
            cancellationToken);

    public Task<ItemRevisado> ExcluirItemAsync(
        string chaveRelease,
        Guid itemId,
        string? motivo,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            revisorId,
            (release, revisor, agora) => release.ExcluirItem(itemId, revisor, agora, motivo),
            cancellationToken);

    public Task<ItemRevisado> ReincluirItemAsync(
        string chaveRelease,
        Guid itemId,
        Guid revisorId,
        CancellationToken cancellationToken = default) =>
        RevisarItemAsync(
            chaveRelease,
            itemId,
            revisorId,
            (release, revisor, agora) => release.ReincluirItem(itemId, revisor, agora),
            cancellationToken);

    private Task<ItemRevisado> RevisarItemAsync(
        string chaveRelease,
        Guid itemId,
        Guid revisorId,
        Func<Release, Usuario, DateTimeOffset, ItemComunicado> revisao,
        CancellationToken cancellationToken) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor, agora) => new ItemRevisado(
                revisao(release, revisor, agora),
                release.PublicoDoItem(itemId)),
            cancellationToken);

    private Task RevisarComoAsync(
        string chaveRelease,
        Guid revisorId,
        Action<Release, Usuario, DateTimeOffset> revisao,
        CancellationToken cancellationToken) =>
        RevisarComoAsync(
            chaveRelease,
            revisorId,
            (release, revisor, agora) =>
            {
                revisao(release, revisor, agora);
                return true;
            },
            cancellationToken);

    private async Task<T> RevisarComoAsync<T>(
        string chaveRelease,
        Guid revisorId,
        Func<Release, Usuario, DateTimeOffset, T> revisao,
        CancellationToken cancellationToken)
    {
        var revisor = await UsuarioResponsavel.ObterAtivoAsync(usuarios, revisorId, cancellationToken);

        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        var resultado = revisao(release, revisor, relogio.GetUtcNow());

        await releases.SalvarAsync(release, cancellationToken);

        return resultado;
    }
}
