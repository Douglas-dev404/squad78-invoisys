namespace InvoiSys.Api.Contracts;

public sealed record AprovarIn(string? Publico);

public sealed record ReprovarIn(string? Publico, string? Motivo);

public sealed record ReabrirIn(string? Publico);
