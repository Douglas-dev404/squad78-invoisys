namespace InvoiSys.Domain.Entities;

public sealed record VersaoJira(
    string Nome,
    string? Descricao,
    DateOnly? DataLancamento,
    bool Lancada,
    bool Arquivada);
