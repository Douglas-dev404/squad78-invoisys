using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeUsuarioRepository : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarEmail(email);
        return Task.FromResult(Usuarios.FirstOrDefault(u => u.Email == normalizado));
    }

    public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Usuarios.Count > 0);

    public Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        if (!Usuarios.Contains(usuario))
        {
            Usuarios.Add(usuario);
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeTokenRedefinicaoSenhaRepository : ITokenRedefinicaoSenhaRepository
{
    public List<TokenRedefinicaoSenha> Tokens { get; } = [];

    public Task AdicionarAsync(TokenRedefinicaoSenha token, CancellationToken cancellationToken = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<TokenRedefinicaoSenha?> BuscarPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<TokenRedefinicaoSenha?> BuscarUltimoDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.Where(t => t.UsuarioId == usuarioId).OrderByDescending(t => t.CriadoEm).FirstOrDefault());

    public Task<bool> ConsumirAsync(Guid id, DateTimeOffset agora, CancellationToken cancellationToken = default)
    {
        var token = Tokens.FirstOrDefault(t => t.Id == id);
        if (token is null || !token.EstaValido(agora))
        {
            return Task.FromResult(false);
        }

        token.MarcarComoUsado(agora);
        return Task.FromResult(true);
    }

    public Task InvalidarPendentesDoUsuarioAsync(Guid usuarioId, DateTimeOffset agora, CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UsuarioId == usuarioId && t.UsadoEm is null))
        {
            token.MarcarComoUsado(agora);
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeEnvioEmail : IEnvioEmail
{
    public List<(string Email, string Nome, string Link, DateTimeOffset ExpiraEm)> Enviados { get; } = [];

    public Task EnviarRedefinicaoSenhaAsync(string email, string nome, string link, DateTimeOffset expiraEm, CancellationToken cancellationToken = default)
    {
        Enviados.Add((email, nome, link, expiraEm));
        return Task.CompletedTask;
    }
}

public sealed class FakeHashSenha : IHashSenha
{
    public string Gerar(string senha) => $"hash:{senha}";

    public bool Verificar(string senha, string hash) => hash == $"hash:{senha}";
}
