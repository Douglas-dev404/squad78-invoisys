using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ComunicadoExportadoRepository(InvoiSysDbContext contexto) : IComunicadoExportadoRepository
{
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
        var consulta = ConsultaComRelease();

        if (publico is not null)
        {
            consulta = consulta.Where(x => x.Exportado.Publico == publico);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var padrao = $"%{EscaparLike(busca.Trim())}%";
            consulta = consulta.Where(x =>
                EF.Functions.ILike(x.ChaveRelease, padrao, "\\")
                || (x.TituloExecutivo != null && EF.Functions.ILike(x.TituloExecutivo, padrao, "\\")));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .OrderByDescending(x => x.Exportado.GeradoEm)
            .ThenByDescending(x => x.Exportado.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(x => new ExportacaoResumo(
                x.Exportado.Id,
                x.ChaveRelease,
                x.TituloExecutivo,
                x.Exportado.Publico,
                x.Exportado.Formato,
                x.Exportado.GeradoPor,
                x.Exportado.GeradoEm,
                null))
            .ToListAsync(cancellationToken);

        return new PaginaExportacoes(itens, total);
    }

    public async Task<ExportacaoResumo?> BuscarDetalheAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await ConsultaComRelease()
            .Where(x => x.Exportado.Id == id)
            .Select(x => new ExportacaoResumo(
                x.Exportado.Id,
                x.ChaveRelease,
                x.TituloExecutivo,
                x.Exportado.Publico,
                x.Exportado.Formato,
                x.Exportado.GeradoPor,
                x.Exportado.GeradoEm,
                x.Exportado.Conteudo))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<ExportadoComRelease> ConsultaComRelease() =>
        from exportado in _contexto.ComunicadosExportados.AsNoTracking()
        join release in _contexto.Releases.AsNoTracking() on exportado.ReleaseId equals release.Id
        join versao in _contexto.VersoesComunicado.AsNoTracking() on exportado.VersaoComunicadoId equals versao.Id
        select new ExportadoComRelease
        {
            Exportado = exportado,
            ChaveRelease = release.ChaveJira,
            TituloExecutivo = versao.TituloExecutivo,
        };

    // Sem isso, "%" ou "_" digitados na busca viram curinga do LIKE.
    private static string EscaparLike(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // Inicializador de membros (não construtor) para o EF conseguir traduzir os Where/OrderBy seguintes.
    private sealed class ExportadoComRelease
    {
        public required ComunicadoExportado Exportado { get; init; }

        public required string ChaveRelease { get; init; }

        public string? TituloExecutivo { get; init; }
    }
}
