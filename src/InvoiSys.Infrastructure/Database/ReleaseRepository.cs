using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ReleaseRepository(InvoiSysDbContext contexto) : IReleaseRepository
{
    private readonly InvoiSysDbContext _contexto = contexto;

    public async Task<Release?> BuscarPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await ConsultaComAgregadoCompleto()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default) =>
        await ConsultaComAgregadoCompleto()
            .FirstOrDefaultAsync(r => r.ChaveJira == chaveJira, cancellationToken);

    public async Task SalvarAsync(Release release, CancellationToken cancellationToken = default)
    {
        if (_contexto.Entry(release).State == EntityState.Detached)
        {
            _contexto.Releases.Add(release);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReleaseResumo>> ListarResumosAsync(
        StatusPipeline? status,
        CancellationToken cancellationToken = default)
    {
        var consulta = _contexto.Releases.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            consulta = consulta.Where(r => r.Status == status);
        }

        return await consulta
            .OrderByDescending(r => EF.Property<DateTimeOffset>(r, "CriadoEm"))
            .Select(r => new ReleaseResumo(
                r.ChaveJira,
                r.Status,
                EF.Property<DateTimeOffset>(r, "CriadoEm")))
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardDados> BuscarDashboardAsync(
        DateOnly hojeUtc,
        CancellationToken cancellationToken = default)
    {
        var inicio = hojeUtc.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = hojeUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var releases = _contexto.Releases.AsNoTracking();
        var versoes = _contexto.VersoesComunicado.AsNoTracking();
        var itens = _contexto.ItensComunicado.AsNoTracking();

        var aprovadas = await releases.CountAsync(r => r.Status == StatusPipeline.Aprovado, cancellationToken);
        var aguardando = await releases.CountAsync(r => r.Status == StatusPipeline.AguardandoRevisao, cancellationToken);
        var falharam = await releases.CountAsync(r => r.Status == StatusPipeline.Falhou, cancellationToken);
        var exportacoes = await _contexto.ComunicadosExportados.CountAsync(cancellationToken);
        var itensGerados = await itens.CountAsync(cancellationToken);
        var itensEditados = await itens.CountAsync(i => i.TextoEditadoManualmente != null, cancellationToken);
        var itensExcluidos = await itens.CountAsync(i => !i.Incluido, cancellationToken);

        var geradas = await releases
            .Where(r => EF.Property<DateTimeOffset>(r, "CriadoEm") >= inicio && EF.Property<DateTimeOffset>(r, "CriadoEm") < fim)
            .GroupBy(r => EF.Property<DateTimeOffset>(r, "CriadoEm").Date)
            .Select(grupo => new { Data = grupo.Key, Total = grupo.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Data), x => x.Total, cancellationToken);
        var aprovadasPorDia = await versoes
            .Where(v => v.Status == StatusRevisao.Aprovado && v.RevisadoEm != null && v.RevisadoEm >= inicio && v.RevisadoEm < fim)
            .GroupBy(v => v.RevisadoEm!.Value.Date)
            .Select(grupo => new { Data = grupo.Key, Total = grupo.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Data), x => x.Total, cancellationToken);
        var ultimosDias = Enumerable.Range(0, 7).Select(indice => hojeUtc.AddDays(indice - 6))
            .Select(data => new DashboardDia(data, aprovadasPorDia.GetValueOrDefault(data), geradas.GetValueOrDefault(data)))
            .ToList();
        var recentes = await releases.OrderByDescending(r => EF.Property<DateTimeOffset>(r, "CriadoEm")).Take(10)
            .Select(r => new DashboardReleaseRecente(r.ChaveJira, r.Status,
                _contexto.VersoesComunicado.Where(v => v.ReleaseId == r.Id && v.Publico == PublicoAlvo.Cliente).Select(v => v.TituloExecutivo).FirstOrDefault(),
                EF.Property<DateTimeOffset>(r, "CriadoEm")))
            .ToListAsync(cancellationToken);

        return new DashboardDados(aprovadas, aguardando, falharam, exportacoes, itensGerados, itensEditados, itensExcluidos, ultimosDias, recentes);
    }

    // Sem lazy loading: faltar um Include devolve coleção vazia em silêncio.
    private IQueryable<Release> ConsultaComAgregadoCompleto() =>
        _contexto.Releases
            .Include(r => r.Historias)
            .Include(r => r.Versoes)
                .ThenInclude(v => v.Itens)
            .Include(r => r.Execucoes);
}
