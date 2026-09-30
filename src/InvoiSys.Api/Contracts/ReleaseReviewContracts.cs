namespace InvoiSys.Api.Contracts;

public sealed record AprovarReleaseRequest(
    string? Publico,
    string AprovadoPor);

public sealed record ReprovarReleaseRequest(
    string? Publico,
    string Motivo,
    string RevisadoPor);

public sealed record ReabrirReleaseRequest(
    string? Publico);