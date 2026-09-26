using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de leitura de <see cref="DestaqueHero"/>, o conteúdo institucional da tela de
/// login (GET /branding/highlights). Agregado independente, sem relação com o pipeline.
///
/// Somente consulta: a única leitura que existe é a lista exibida no login. Não há ainda
/// tela nem regra para cadastrar/editar destaques; uma porta de escrita agora seria
/// antecipar um caso de uso que ninguém pediu.
/// </summary>
public interface IDestaqueHeroRepository
{
    /// <summary>
    /// Lista os destaques ativos, na ordem de exibição (<see cref="DestaqueHero.Ordem"/>,
    /// menor primeiro). Devolve lista vazia se não houver nenhum ativo.
    /// </summary>
    Task<IReadOnlyList<DestaqueHero>> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
