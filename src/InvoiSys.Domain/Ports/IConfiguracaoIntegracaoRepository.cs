using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IConfiguracaoIntegracaoRepository
{
    Task<IReadOnlyList<ConfiguracaoIntegracao>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Grava (insere ou atualiza) e remove as chaves em <paramref name="remover"/>, numa transação só.</summary>
    Task SalvarAsync(
        IReadOnlyCollection<ConfiguracaoIntegracao> gravar,
        IReadOnlyCollection<string> remover,
        CancellationToken cancellationToken = default);
}
