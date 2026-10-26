using System.Security.Cryptography;
using System.Text;
using InvoiSys.Domain.Ports;
using Npgsql;

namespace InvoiSys.Infrastructure.Database;

public sealed class PostgresBloqueioProcessamento(NpgsqlDataSource dataSource) : IBloqueioProcessamento
{
    public async Task<IAsyncDisposable?> TentarAdquirirAsync(
        string chaveRelease,
        CancellationToken cancellationToken = default)
    {
        var chave = CalcularChave(chaveRelease);
        var conexao = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var comando = new NpgsqlCommand("SELECT pg_try_advisory_lock(@chave)", conexao);
            comando.Parameters.AddWithValue("chave", chave);

            var obtido = (bool)(await comando.ExecuteScalarAsync(cancellationToken))!;

            if (obtido)
            {
                return new Trava(conexao, chave);
            }
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }

        await conexao.DisposeAsync();
        return null;
    }

    internal static long CalcularChave(string chaveRelease)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes($"invoisys:processar-release:{chaveRelease.Trim().ToUpperInvariant()}"));
        return BitConverter.ToInt64(bytes, 0);
    }

    private sealed class Trava(NpgsqlConnection conexao, long chave) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var comando = new NpgsqlCommand("SELECT pg_advisory_unlock(@chave)", conexao);
                comando.Parameters.AddWithValue("chave", chave);
                await comando.ExecuteScalarAsync(CancellationToken.None);
            }
            finally
            {
                await conexao.DisposeAsync();
            }
        }
    }
}
