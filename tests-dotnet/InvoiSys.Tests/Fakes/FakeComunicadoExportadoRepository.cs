using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeComunicadoExportadoRepository : IComunicadoExportadoRepository
{
    private readonly List<ComunicadoExportado> _exportados = [];

    public IReadOnlyList<ComunicadoExportado> Exportados => _exportados;

    public Task AdicionarAsync(
        ComunicadoExportado exportado,
        CancellationToken cancellationToken = default)
    {
        _exportados.Add(exportado);
        return Task.CompletedTask;
    }

    public Task<ComunicadoExportado?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_exportados.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ComunicadoExportado>>([.. _exportados
            .Where(e => e.ReleaseId == releaseId && (publico is null || e.Publico == publico))
            .OrderByDescending(e => e.GeradoEm)]);

    // Sem Release carregada aqui: a chave vira o ReleaseId e o título fica nulo.
    public Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        var filtrados = _exportados
            .Where(e => publico is null || e.Publico == publico)
            .Select(Resumo)
            .Where(r => string.IsNullOrWhiteSpace(busca)
                || r.ChaveRelease.Contains(busca, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.GeradoEm)
            .ThenByDescending(r => r.Id)
            .ToList();

        return Task.FromResult(new PaginaExportacoes(
            [.. filtrados.Skip((pagina - 1) * tamanho).Take(tamanho).Select(r => r with { Conteudo = null })],
            filtrados.Count));
    }

    public Task<ExportacaoResumo?> BuscarDetalheAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_exportados.Where(e => e.Id == id).Select(Resumo).FirstOrDefault());

    private static ExportacaoResumo Resumo(ComunicadoExportado e) => new(
        e.Id, e.ReleaseId.ToString(), null, e.Publico, e.Formato, e.GeradoPor, e.GeradoEm, e.Conteudo);
}
