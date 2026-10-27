namespace InvoiSys.Api.Contracts;

public sealed record AprovarIn(string? Publico, string? AprovadoPor);

public sealed record ReprovarIn(string? Publico, string? Motivo, string? RevisadoPor);

public sealed record ReabrirIn(string? Publico);
