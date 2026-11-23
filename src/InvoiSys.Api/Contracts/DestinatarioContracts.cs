using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Contracts;

public sealed record ListarDestinatariosIn(string? Publico, bool? IncluirInativos);

public sealed record SalvarDestinatarioIn(string? Nome, string? Email, string? Publico);

public sealed record DestinatarioOut(
    Guid Id,
    string Nome,
    string Email,
    string Publico,
    bool Ativo,
    DateTimeOffset CriadoEm)
{
    public static DestinatarioOut De(Destinatario destinatario) => new(
        destinatario.Id,
        destinatario.Nome,
        destinatario.Email,
        destinatario.Publico.ParaValor(),
        destinatario.Ativo,
        destinatario.CriadoEm);
}
