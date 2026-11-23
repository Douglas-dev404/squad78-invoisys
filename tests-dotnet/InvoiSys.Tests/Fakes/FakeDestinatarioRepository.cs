using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeDestinatarioRepository : IDestinatarioRepository
{
    public List<Destinatario> Destinatarios { get; } = [];

    public FakeDestinatarioRepository Com(params Destinatario[] destinatarios)
    {
        Destinatarios.AddRange(destinatarios);
        return this;
    }

    public Task<Destinatario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Destinatarios.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Destinatario>> BuscarPorIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Destinatario>>([.. Destinatarios.Where(d => ids.Contains(d.Id))]);

    public Task<bool> ExisteEmailNoPublicoAsync(
        string email,
        PublicoAlvo publico,
        Guid? ignorarId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return Task.FromResult(Destinatarios.Any(
            d => d.Email == normalizado && d.Publico == publico && d.Id != ignorarId));
    }

    public Task<IReadOnlyList<Destinatario>> ListarAsync(
        PublicoAlvo? publico,
        bool incluirInativos,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Destinatario>>([.. Destinatarios
            .Where(d => (publico is null || d.Publico == publico) && (incluirInativos || d.Ativo))
            .OrderBy(d => d.Nome)
            .ThenBy(d => d.Email)]);

    public Task SalvarAsync(Destinatario destinatario, CancellationToken cancellationToken = default)
    {
        if (!Destinatarios.Contains(destinatario))
        {
            Destinatarios.Add(destinatario);
        }

        return Task.CompletedTask;
    }
}
