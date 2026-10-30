using InvoiSys.Domain.Entities;

namespace InvoiSys.Api.Contracts;

public sealed record LoginIn(string? Email, string? Senha);

public sealed record UsuarioOut(
    Guid Id, string Nome, string Email, string Papel, string? AvatarUrl, DateTimeOffset CriadoEm)
{
    public static UsuarioOut De(Usuario u) =>
        new(u.Id, u.Nome, u.Email, u.Papel, u.AvatarUrl, u.CriadoEm);
}

public sealed record LoginOut(UsuarioOut Usuario, string Token, DateTimeOffset ExpiraEm);