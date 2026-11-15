using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeComunicadoExportadoRepository : IComunicadoExportadoRepository
{
    private readonly List<ComunicadoExportado> _exportados = [];
    private readonly Dictionary<Guid, string> _chavesPorRelease = [];
    private readonly Dictionary<Guid, string?> _titulosPorVersao = [];

    public IReadOnlyList<ComunicadoExportado> Exportados => _exportados;

    public FakeComunicadoExportadoRepository ComRelease(Release release)
    {
        _chavesPorRelease[release.Id] = release.ChaveJira;

        foreach (var versao in release.Versoes)
        {
            _titulosPorVersao[versao.Id] = versao.TituloExecutivo;
        }

        return this;
    }

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

    public Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        var filtrados = _exportados
            .Select(Detalhar)
            .Where(d => publico is null || d.Publico == publico)
            .Where(d => string.IsNullOrWhiteSpace(busca)
                || d.ChaveRelease.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase)
                || (d.TituloExecutivo?.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(d => d.GeradoEm)
            .ThenByDescending(d => d.Id)
            .ToList();

        return Task.FromResult(new PaginaExportacoes(
            [.. filtrados
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .Select(d => new ExportacaoResumo(
                    d.Id, d.ChaveRelease, d.TituloExecutivo, d.Publico, d.Formato, d.GeradoPor, d.GeradoEm))],
            filtrados.Count));
    }

    public Task<ExportacaoDetalhe?> BuscarDetalheAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_exportados.Where(e => e.Id == id).Select(Detalhar).FirstOrDefault());

    private ExportacaoDetalhe Detalhar(ComunicadoExportado e) => new(
        e.Id,
        _chavesPorRelease.GetValueOrDefault(e.ReleaseId, string.Empty),
        _titulosPorVersao.GetValueOrDefault(e.VersaoComunicadoId),
        e.Publico,
        e.Formato,
        e.GeradoPor,
        e.GeradoEm,
        e.Conteudo);
}
