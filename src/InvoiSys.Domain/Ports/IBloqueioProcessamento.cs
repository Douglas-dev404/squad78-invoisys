namespace InvoiSys.Domain.Ports;

public interface IBloqueioProcessamento
{
    Task<IAsyncDisposable?> TentarAdquirirAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default);
}
