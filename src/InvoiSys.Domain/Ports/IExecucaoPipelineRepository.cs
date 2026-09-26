using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta de <b>leitura isolada</b> de <see cref="ExecucaoPipeline"/>: o histórico de
/// execuções do pipeline de IA (rastreabilidade/logs de geração, requisito do MVP) sem
/// carregar o agregado <see cref="Release"/> inteiro.
///
/// Somente consulta, pelo mesmo motivo de <see cref="IHistoriaJiraRepository"/>:
/// <see cref="ExecucaoPipeline"/> é filha do agregado Release e só nasce por
/// <see cref="Release.RegistrarExecucao"/>. Gravar ou atualizar execução continua
/// passando por <see cref="IReleaseRepository.SalvarAsync"/> — uma porta de escrita aqui
/// deixaria registrar execução de uma Release sem passar pelo domínio dela.
/// </summary>
public interface IExecucaoPipelineRepository
{
    /// <summary>Busca uma execução pela chave técnica. Devolve null se não existir.</summary>
    Task<ExecucaoPipeline?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista as execuções de uma Release, da mais recente para a mais antiga — a ordem
    /// natural de um log. Devolve lista vazia se a Release não existir ou nunca tiver
    /// sido processada.
    /// </summary>
    Task<IReadOnlyList<ExecucaoPipeline>> ListarPorReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);
}
