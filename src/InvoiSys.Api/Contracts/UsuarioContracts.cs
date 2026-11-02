namespace InvoiSys.Api.Contracts;

public sealed record CadastrarUsuarioIn(string? Nome, string? Email, string? Papel, string? SenhaInicial);
public sealed record UsuarioOut(Guid Id, string Nome, string Email, string Papel, bool Ativo, DateTimeOffset CriadoEm);
