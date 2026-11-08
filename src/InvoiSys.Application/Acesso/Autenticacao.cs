using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Acesso;

public sealed record SessaoAutenticada(Usuario Usuario, TokenEmitido Token);

public sealed class Autenticacao(
    IUsuarioRepository usuarios,
    IHashSenha hashSenha,
    IEmissorToken emissor)
{
    internal const string HashFicticio =
        "$argon2id$v=19$m=19456,t=2,p=1$upFHShSIxrRSzQ8Yvkr96g$/fcv9wL7pT9cQLr3jOdWfYX2S3vYMgVKoP+GG9HKbcg";

    public async Task<SessaoAutenticada?> AutenticarAsync(
        string email, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(email, cancellationToken);
        var senhaCorreta = hashSenha.Verificar(senha, usuario?.SenhaHash ?? HashFicticio);

        return usuario is { Ativo: true } && senhaCorreta
            ? new SessaoAutenticada(usuario, emissor.Emitir(usuario))
            : null;
    }

    public async Task<Usuario?> BuscarAtivoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken);
        return usuario is { Ativo: true } ? usuario : null;
    }
}