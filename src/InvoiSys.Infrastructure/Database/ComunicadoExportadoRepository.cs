using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ComunicadoExportadoRepository(InvoiSysDbContext contexto) : IComunicadoExportadoRepository
{
    private const string CaractereDeEscape = "\\";

    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task AdicionarAsync(
        ComunicadoExportado exportado,
        CancellationToken cancellationToken = default)
    {
        _contexto.ComunicadosExportados.Add(exportado);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComunicadoExportado?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _contexto.ComunicadosExportados
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.ComunicadosExportados
            .AsNoTracking()
            .Where(c => c.ReleaseId == releaseId);

        if (publico is not null)
        {
            consulta = consulta.Where(c => c.Publico == publico);
        }

        return await consulta
            .OrderByDescending(c => c.GeradoEm)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        var consulta = ComChaveETitulo();

        if (publico is not null)
        {
            consulta = consulta.Where(l => l.Exportado.Publico == publico);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var padrao = $"%{EscaparCuringas(busca.Trim())}%";
            consulta = consulta.Where(l =>
                EF.Functions.ILike(l.ChaveRelease, padrao, CaractereDeEscape)
                || (l.TituloExecutivo != null
                    && EF.Functions.ILike(l.TituloExecutivo, padrao, CaractereDeEscape)));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var itens = await consulta
            .OrderByDescending(l => l.Exportado.GeradoEm)
            .ThenByDescending(l => l.Exportado.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(l => new ExportacaoResumo(
                l.Exportado.Id,
                l.ChaveRelease,
                l.TituloExecutivo,
                l.Exportado.Publico,
                l.Exportado.Formato,
                l.Exportado.GeradoPor,
                l.Exportado.GeradoEm))
            .ToListAsync(cancellationToken);

        return new PaginaExportacoes(itens, total);
    }

    public async Task<ExportacaoDetalhe?> BuscarDetalheAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await ComChaveETitulo()
            .Where(l => l.Exportado.Id == id)
            .Select(l => new ExportacaoDetalhe(
                l.Exportado.Id,
                l.ChaveRelease,
                l.TituloExecutivo,
                l.Exportado.Publico,
                l.Exportado.Formato,
                l.Exportado.GeradoPor,
                l.Exportado.GeradoEm,
                l.Exportado.Conteudo))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<LinhaExportacao> ComChaveETitulo() =>
        from exportado in _contexto.ComunicadosExportados.AsNoTracking()
        join release in _contexto.Releases.AsNoTracking() on exportado.ReleaseId equals release.Id
        join versao in _contexto.VersoesComunicado.AsNoTracking() on exportado.VersaoComunicadoId equals versao.Id
        select new LinhaExportacao
        {
            Exportado = exportado,
            ChaveRelease = release.ChaveJira,
            TituloExecutivo = versao.TituloExecutivo,
        };

    private static string EscaparCuringas(string texto) => texto
        .Replace(CaractereDeEscape, CaractereDeEscape + CaractereDeEscape, StringComparison.Ordinal)
        .Replace("%", CaractereDeEscape + "%", StringComparison.Ordinal)
        .Replace("_", CaractereDeEscape + "_", StringComparison.Ordinal);

    private sealed class LinhaExportacao
    {
        public required ComunicadoExportado Exportado { get; init; }

        public required string ChaveRelease { get; init; }

        public string? TituloExecutivo { get; init; }
    }
}
