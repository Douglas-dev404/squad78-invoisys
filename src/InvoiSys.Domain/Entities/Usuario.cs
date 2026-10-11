namespace InvoiSys.Domain.Entities;

public sealed class Usuario
{
    public const string PapelAdmin = "admin";

    public Usuario(string nome, string email, string senhaHash, string papel)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Email = NormalizarEmail(email);
        SenhaHash = senhaHash;
        Papel = papel;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public string SenhaHash { get; private set; }

    public string Papel { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public static string NormalizarEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public void Desativar() => Ativo = false;

    public void AtualizarAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;
}
