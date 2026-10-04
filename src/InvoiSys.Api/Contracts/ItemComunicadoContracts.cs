namespace InvoiSys.Api.Contracts;

public sealed record EditarItemIn(string? Texto);

public sealed record ExcluirItemIn(string? Motivo);

public sealed record ItemRevisaoOut(
    Guid Id,
    string Publico,
    string Categoria,
    string Texto,
    string? TextoEditadoManualmente,
    string TextoFinal,
    bool Incluido,
    string? MotivoExclusao,
    IReadOnlyList<string> Origens);
