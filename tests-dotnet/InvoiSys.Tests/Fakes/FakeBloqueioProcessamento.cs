using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeBloqueioProcessamento : IBloqueioProcessamento
{
    private readonly HashSet<string> _ocupadas = [];
    private readonly Lock _trava = new();

    public int Liberacoes { get; private set; }

    public Task<IAsyncDisposable?> TentarAdquirirAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default)
    {
        lock (_trava)
        {
            if (!_ocupadas.Add(chaveRelease))
            {
                return Task.FromResult<IAsyncDisposable?>(null);
            }
        }

        return Task.FromResult<IAsyncDisposable?>(new Liberador(this, chaveRelease));
    }

    public bool EstaOcupada(string chaveRelease)
    {
        lock (_trava)
        {
            return _ocupadas.Contains(chaveRelease);
        }
    }

    public void Ocupar(string chaveRelease)
    {
        lock (_trava)
        {
            _ocupadas.Add(chaveRelease);
        }
    }

    private sealed class Liberador(FakeBloqueioProcessamento dono, string chave) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            lock (dono._trava)
            {
                dono._ocupadas.Remove(chave);
                dono.Liberacoes++;
            }

            return ValueTask.CompletedTask;
        }
    }
}
