using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Revisao;

/// <summary>
/// Casos de uso da revisão humana de um comunicado, por público: aprovar, reprovar e
/// reabrir. Todos seguem o mesmo caminho — carrega a Release, chama o método do
/// agregado, salva. A regra (transições de status, quando a Release fica Aprovada)
/// mora no domínio; aqui só a orquestração.
/// </summary>
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

    private async Task RevisarAsync(
        string chaveRelease,
        Action<Release> revisao,
        CancellationToken cancellationToken)
    {
        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        revisao(release);

        await releases.SalvarAsync(release, cancellationToken);
    }
}
