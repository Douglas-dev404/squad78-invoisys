using InvoiSys.Domain.Ports;

namespace InvoiSys.Infrastructure.Integracoes;

public sealed class MonitorExecucaoEmMemoria(TimeProvider relogio) : IMonitorExecucao
{
    private const int Capacidade = 400;
    private const int LimiteTexto = 8000;

    private readonly AsyncLocal<string?> _releaseAtual = new();
    private readonly Lock _trava = new();
    private readonly List<EventoExecucao> _eventos = [];
    private long _ultimoId;

    public IDisposable IniciarEscopo(string chaveRelease)
    {
        var anterior = _releaseAtual.Value;
        _releaseAtual.Value = chaveRelease;
        return new Escopo(() => _releaseAtual.Value = anterior);
    }

    public void Publicar(
        string tipo,
        string mensagem,
        string? prompt = null,
        string? resposta = null,
        string? modelo = null,
        long? duracaoMs = null)
    {
        lock (_trava)
        {
            _eventos.Add(new EventoExecucao(
                ++_ultimoId,
                relogio.GetUtcNow(),
                _releaseAtual.Value,
                tipo,
                mensagem,
                Cortar(prompt),
                Cortar(resposta),
                modelo,
                duracaoMs));

            if (_eventos.Count > Capacidade)
            {
                _eventos.RemoveRange(0, _eventos.Count - Capacidade);
            }
        }
    }

    public IReadOnlyList<EventoExecucao> Depois(long ultimoId, string? release = null)
    {
        lock (_trava)
        {
            return [.. _eventos.Where(e =>
                e.Id > ultimoId
                && (string.IsNullOrEmpty(release)
                    || string.Equals(e.Release, release, StringComparison.OrdinalIgnoreCase)))];
        }
    }

    private static string? Cortar(string? texto) =>
        texto is { Length: > LimiteTexto } ? texto[..LimiteTexto] + "… (cortado)" : texto;

    private sealed class Escopo(Action aoFechar) : IDisposable
    {
        public void Dispose() => aoFechar();
    }
}
