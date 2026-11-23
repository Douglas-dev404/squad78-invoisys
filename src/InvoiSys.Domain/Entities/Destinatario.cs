using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class Destinatario
{
    public const int TamanhoMaximoNome = 200;

    public const int TamanhoMaximoEmail = 320;

    private Destinatario()
    {
        Nome = string.Empty;
        Email = string.Empty;
    }

    public Destinatario(string nome, string email, PublicoAlvo publico)
    {
        Id = Guid.NewGuid();
        (Nome, Email) = Validar(nome, email);
        Publico = publico;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public PublicoAlvo Publico { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public void Alterar(string nome, string email, PublicoAlvo publico)
    {
        (Nome, Email) = Validar(nome, email);
        Publico = publico;
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    private static (string Nome, string Email) Validar(string nome, string email)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome do destinatário é obrigatório.", nameof(nome));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("E-mail do destinatário é obrigatório.", nameof(email));
        }

        return (nome.Trim(), Usuario.NormalizarEmail(email));
    }
}
