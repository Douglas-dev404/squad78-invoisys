using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Database;

public sealed class ConfiguracaoIntegracaoRepository(InvoiSysDbContext contexto) : IConfiguracaoIntegracaoRepository
{
    public async Task<IReadOnlyList<ConfiguracaoIntegracao>> ListarAsync(
        CancellationToken cancellationToken = default) =>
        await contexto.ConfiguracoesIntegracao.AsNoTracking().ToListAsync(cancellationToken);

    public async Task SalvarAsync(
        IReadOnlyCollection<ConfiguracaoIntegracao> gravar,
        IReadOnlyCollection<string> remover,
        CancellationToken cancellationToken = default)
    {
        var chaves = gravar.Select(c => c.Chave).Concat(remover).ToList();
        var existentes = await contexto.ConfiguracoesIntegracao
            .Where(c => chaves.Contains(c.Chave))
            .ToDictionaryAsync(c => c.Chave, cancellationToken);

        foreach (var nova in gravar)
        {
            if (existentes.TryGetValue(nova.Chave, out var atual))
            {
                atual.Atualizar(nova.Valor);
            }
            else
            {
                contexto.ConfiguracoesIntegracao.Add(nova);
            }
        }

        foreach (var chave in remover)
        {
            if (existentes.TryGetValue(chave, out var atual))
            {
                contexto.ConfiguracoesIntegracao.Remove(atual);
            }
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }
}
