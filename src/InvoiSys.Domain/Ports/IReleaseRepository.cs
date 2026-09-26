using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de persistência do agregado <see cref="Release"/>. Desacopla o pipeline/API do
/// EF Core concreto — a implementação real vive em InvoiSys.Infrastructure.Database.
///
/// Exemplo de referência do padrão de Repository deste projeto: as entidades filhas
/// (VersaoComunicado, ItemComunicado, ExecucaoPipeline, HistoriaJira) não têm repository
/// de escrita próprio porque só existem dentro do agregado Release — persistir/carregar
/// a Release já persiste/carrega as filhas junto, via as navegações que o EF Core
/// materializa (ver <c>ReleaseConfiguration</c>). Repository é por agregado, não por
/// tabela. HistoriaJira e ExecucaoPipeline têm, além disso, portas somente leitura para
/// consulta isolada.
/// </summary>
public interface IReleaseRepository
{
    /// <summary>
    /// Busca uma Release pela chave técnica, com histórias, versões e execuções já
    /// carregadas — o agregado completo, pronto para os métodos de ciclo de vida
    /// (Aprovar, Reprovar, ...) operarem sem lazy loading surpresa. Devolve null se não
    /// existir; decidir se isso é 404 é responsabilidade de quem chama, não da porta.
    /// </summary>
    Task<Release?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma Release pela chave de negócio do Jira (ex. "RELEASE-2026-08"), mesmo
    /// carregamento completo de <see cref="BuscarPorIdAsync"/>. É o caminho que a API
    /// usa de fato — a rota recebe a chave, não o uuid técnico.
    /// </summary>
    Task<Release?> BuscarPorChaveJiraAsync(
        string chaveJira,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste uma Release nova ou existente. Sem distinção Add/Update explícita: o EF
    /// Core decide pelo estado de rastreamento da entidade, e expor essa distinção na
    /// porta vazaria detalhe de ORM para o domínio/aplicação.
    /// </summary>
    Task SalvarAsync(Release release, CancellationToken cancellationToken = default);
}
