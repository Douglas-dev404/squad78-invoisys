namespace InvoiSys.Api.Contracts;

public sealed record VersaoJiraOut(
    string Nome,
    bool Lancada,
    bool Arquivada,
    DateOnly? DataLancamento);
