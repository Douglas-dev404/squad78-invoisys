namespace InvoiSys.Domain.Entities;

public sealed class TokenRedefinicaoInvalidoException()
    : Exception("Token de redefinição inválido, expirado ou já utilizado.");

public sealed class TokenRedefinicaoSenha
{
    public const int ValidadeMinutos = 30;

    private TokenRedefinicaoSenha()
    {
        TokenHash = string.Empty;
    }

    private TokenRedefinicaoSenha(Guid usuarioId, string tokenHash, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        CriadoEm = agora;
        ExpiraEm = agora.AddMinutes(ValidadeMinutos);
    }

    public Guid Id { get; private init; }

    public Guid UsuarioId { get; private init; }

    public string TokenHash { get; private init; }

    public DateTimeOffset CriadoEm { get; private init; }

    public DateTimeOffset ExpiraEm { get; private init; }

    public DateTimeOffset? UsadoEm { get; private set; }

    public static TokenRedefinicaoSenha Emitir(Guid usuarioId, string tokenHash, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("O hash do token é obrigatório.", nameof(tokenHash));
        }

        return new TokenRedefinicaoSenha(usuarioId, tokenHash, agora);
    }

    public bool EstaValido(DateTimeOffset agora) => UsadoEm is null && agora < ExpiraEm;

    public void MarcarComoUsado(DateTimeOffset agora)
    {
        if (!EstaValido(agora))
        {
            throw new TokenRedefinicaoInvalidoException();
        }

        UsadoEm = agora;
    }
}
