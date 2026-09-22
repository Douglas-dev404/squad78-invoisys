using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

/// <summary>
/// Implementação real de <see cref="IReleaseRepository"/> via EF Core.
///
/// Serve de <b>exemplo de referência</b> do padrão de Repository deste projeto — as
/// demais entidades do agregado (<see cref="VersaoComunicado"/>,
/// <see cref="ExecucaoPipeline"/>, ...) não ganham repository próprio: elas só existem
/// dentro do agregado <see cref="Release"/>, então carregar/salvar a Release já
/// carrega/salva as filhas junto, via as navegações mapeadas em
/// <c>ReleaseConfiguration</c>. Um novo agregado raiz (por exemplo, se `Usuario` um dia
/// tiver ciclo de vida próprio) repete este mesmo padrão: uma porta em
/// InvoiSys.Domain.Ports, uma implementação aqui, registrada no composition root.
/// </summary>
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
        // Sem distinção explícita Add/Update: o ChangeTracker do EF Core já sabe se
        // esta instância veio de uma consulta (rastreada, vira UPDATE) ou é nova
        // (não rastreada, vira INSERT) — decidir isso aqui duplicaria uma decisão que
        // o próprio EF já toma corretamente, e criaria uma chance real de marcar
        // "Added" uma Release que já existe, duplicando a linha.
        if (_contexto.Entry(release).State == EntityState.Detached)
        {
            _contexto.Releases.Add(release);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Include explícito das três coleções do agregado, mais os itens de cada versão
    /// (nível 2). Sem isso, acessar <c>release.Versoes[0].Itens</c> depois de carregar
    /// devolveria lista vazia em silêncio (lazy loading não está habilitado neste
    /// projeto) — pior que uma exceção, porque parece dado válido.
    /// </summary>
    private IQueryable<Release> ConsultaComAgregadoCompleto() =>
        _contexto.Releases
            .Include(r => r.Historias)
            .Include(r => r.Versoes)
                .ThenInclude(v => v.Itens)
            .Include(r => r.Execucoes);
}
