namespace InvoiSys.Api.Contracts;

public sealed record EsqueciSenhaIn(string? Email);

public sealed record EsqueciSenhaOut(string Mensagem, int ExpiraEmMinutos);

public sealed record RedefinirSenhaIn(string? Token, string? NovaSenha);
