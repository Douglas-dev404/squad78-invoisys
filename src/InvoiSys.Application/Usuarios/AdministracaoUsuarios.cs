using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Usuarios;

public sealed class AdministracaoUsuarios(IUsuarioRepository usuarios, IHashSenha hashSenha)
{
    public async Task<Usuario> CadastrarAsync(
        string nome, string email, string papel, string senhaInicial,
        CancellationToken cancellationToken = default)
    {
        if (!Usuario.PapelEhValido(papel))
            throw new PapelUsuarioInvalidoException(papel);
        if (string.IsNullOrWhiteSpace(senhaInicial) || senhaInicial.Length < 8)
            throw new SenhaInicialInvalidaException();
        if (await usuarios.BuscarPorEmailAsync(email, cancellationToken) is not null)
            throw new EmailUsuarioDuplicadoException(email);

        var usuario = new Usuario(nome.Trim(), email, hashSenha.Gerar(senhaInicial), papel);
        await usuarios.SalvarAsync(usuario, cancellationToken);
        return usuario;
    }

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        usuarios.ListarAsync(cancellationToken);

    public async Task DesativarAsync(Guid id, Guid administradorId, CancellationToken cancellationToken = default)
    {
        if (id == administradorId)
            throw new AutoDesativacaoUsuarioException();
        var usuario = await usuarios.BuscarPorIdAsync(id, cancellationToken)
            ?? throw new UsuarioNaoEncontradoException(id);
        usuario.Desativar();
        await usuarios.SalvarAsync(usuario, cancellationToken);
    }
}

public sealed class PapelUsuarioInvalidoException(string? papel)
    : Exception($"Papel '{papel}' inválido. Valores aceitos: admin, release_manager.");
public sealed class SenhaInicialInvalidaException()
    : Exception("A senha inicial deve ter pelo menos 8 caracteres.");
public sealed class EmailUsuarioDuplicadoException(string email)
    : Exception($"Já existe usuário cadastrado com o e-mail '{email}'.");
public sealed class UsuarioNaoEncontradoException(Guid id)
    : Exception($"Usuário '{id}' não encontrado.");
public sealed class AutoDesativacaoUsuarioException()
    : Exception("Um administrador não pode desativar a própria conta.");
