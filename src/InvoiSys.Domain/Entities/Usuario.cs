namespace InvoiSys.Domain.Entities;

/// <summary>
/// Usuário autenticado da plataforma — quem faz login, revisa e aprova Releases.
///
/// Papel (<see cref="Papel"/>) é texto livre, não enum: o RBAC do produto ainda não
/// tem um conjunto fechado de papéis definido (o frontend hoje só usa
/// "release_manager" como exemplo), travar em enum agora seria antecipar uma regra
/// de negócio que ninguém validou ainda.
/// </summary>
public sealed class Usuario
{
    public Usuario(string nome, string email, string senhaHash, string papel)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        Papel = papel;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public string Nome { get; private set; }

    /// <summary>E-mail corporativo — identificador de login, único.</summary>
    public string Email { get; private set; }

    /// <summary>Hash da senha (bcrypt/argon2) — nunca texto plano.</summary>
    public string SenhaHash { get; private set; }

    public string Papel { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public void Desativar() => Ativo = false;

    public void AtualizarAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;
}
