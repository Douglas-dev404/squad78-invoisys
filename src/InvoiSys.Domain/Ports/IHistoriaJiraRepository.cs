using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de <b>leitura isolada</b> de <see cref="HistoriaJira"/>: consulta uma história,
/// ou as histórias de uma Release, sem carregar o agregado <see cref="Release"/> inteiro
/// (versões, itens, execuções). É o caminho para quem só precisa das histórias — a
/// alternativa, <see cref="IReleaseRepository"/>, materializa o agregado completo.
///
/// É somente consulta de propósito. <see cref="HistoriaJira"/> é filha do agregado
/// Release: só entra nele pelo construtor (nenhum método de ciclo de vida a adiciona) e a
/// chave estrangeira para a Release é uma shadow property, invisível ao domínio.
/// Persistir ou alterar histórias continua passando por
/// <see cref="IReleaseRepository.SalvarAsync"/>; uma porta de escrita aqui deixaria
/// qualquer camada gravar uma história por fora do agregado.
///
/// As histórias voltam sem rastreamento do EF Core, então não há como salvá-las
/// acidentalmente por esta porta.
/// </summary>
public interface IHistoriaJiraRepository
{
    /// <summary>
    /// Busca uma história pela chave técnica. Devolve null se não existir; decidir se
    /// isso é 404 é responsabilidade de quem chama, não da porta.
    /// </summary>
    Task<HistoriaJira?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma história pela chave do Jira (ex. "INV-1234") <b>dentro de uma Release</b>.
    /// A chave só é única por Release — a mesma issue pode aparecer em outra —, por isso
    /// o <paramref name="releaseId"/> é obrigatório. Devolve null se a Release não tiver
    /// essa chave.
    /// </summary>
    Task<HistoriaJira?> BuscarPorChaveAsync(
        Guid releaseId,
        string chave,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista as histórias de uma Release, ordenadas pela chave do Jira para o resultado
    /// ser estável. Devolve lista vazia se a Release não existir ou não tiver histórias.
    /// </summary>
    Task<IReadOnlyList<HistoriaJira>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);
}
