using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Autenticacao;

public sealed record OpcoesRecuperacaoSenha(string UrlBase, TimeSpan Cooldown);

public sealed class RecuperacaoSenha(
    IUsuarioRepository usuarios,
    ITokenRedefinicaoSenhaRepository tokens,
    IHashSenha hashSenha,
    IEnvioEmail envioEmail,
    OpcoesRecuperacaoSenha opcoes,
    TimeProvider relogio)
{
    public const string MensagemEnvio =
        "Se o e-mail estiver cadastrado, você receberá um link com as instruções para "
        + "redefinir sua senha em instantes.";

    public async Task SolicitarAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("O e-mail é obrigatório.", nameof(email));
        }

        var usuario = await usuarios.BuscarPorEmailAsync(email, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            return;
        }

        var agora = relogio.GetUtcNow();
        var ultimo = await tokens.BuscarUltimoDoUsuarioAsync(usuario.Id, cancellationToken);
        if (ultimo is not null && agora - ultimo.CriadoEm < opcoes.Cooldown)
        {
            return;
        }

        await tokens.InvalidarPendentesDoUsuarioAsync(usuario.Id, agora, cancellationToken);

        var (token, hash) = GeradorTokenRedefinicao.Gerar();
        var emitido = TokenRedefinicaoSenha.Emitir(usuario.Id, hash, agora);
        await tokens.AdicionarAsync(emitido, cancellationToken);

        var link = $"{opcoes.UrlBase.TrimEnd('/')}#token={token}";
        await envioEmail.EnviarRedefinicaoSenhaAsync(
            usuario.Email, usuario.Nome, link, emitido.ExpiraEm, cancellationToken);
    }

    public Task ReenviarAsync(string? email, CancellationToken cancellationToken = default) =>
        SolicitarAsync(email, cancellationToken);

    public async Task RedefinirAsync(
        string? token,
        string? novaSenha,
        CancellationToken cancellationToken = default)
    {
        Usuario.ValidarSenhaNova(novaSenha);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new TokenRedefinicaoInvalidoException();
        }

        var agora = relogio.GetUtcNow();
        var registro = await tokens.BuscarPorHashAsync(
            GeradorTokenRedefinicao.CalcularHash(token.Trim()), cancellationToken);
        if (registro is null || !registro.EstaValido(agora))
        {
            throw new TokenRedefinicaoInvalidoException();
        }

        var usuario = await usuarios.BuscarPorIdAsync(registro.UsuarioId, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new TokenRedefinicaoInvalidoException();
        }

        if (!await tokens.ConsumirAsync(registro.Id, agora, cancellationToken))
        {
            throw new TokenRedefinicaoInvalidoException();
        }

        usuario.AlterarSenhaHash(hashSenha.Gerar(novaSenha!));
        await usuarios.SalvarAsync(usuario, cancellationToken);
        await tokens.InvalidarPendentesDoUsuarioAsync(usuario.Id, agora, cancellationToken);
    }
}
