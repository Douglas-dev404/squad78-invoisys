using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeEnvioComunicadoRepository : IEnvioComunicadoRepository
{
    public List<EnvioComunicado> Envios { get; } = [];

    public Task AdicionarAsync(IReadOnlyCollection<EnvioComunicado> envios, CancellationToken cancellationToken = default)
    {
        Envios.AddRange(envios);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EnvioComunicado>> ListarPorExportacaoAsync(
        Guid comunicadoExportadoId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EnvioComunicado>>([.. Envios
            .Where(e => e.ComunicadoExportadoId == comunicadoExportadoId)
            .OrderBy(e => e.Nome)
            .ThenBy(e => e.Email)]);
}
