using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

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
    IReadOnlyList<string> Origens)
{
    public static ItemRevisaoOut De(ItemComunicado item, PublicoAlvo publico) => new(
        item.Id,
        publico.ParaValor(),
        item.Categoria.ParaValor(),
        item.Texto,
        item.TextoEditadoManualmente,
        item.TextoFinal,
        item.Incluido,
        item.MotivoExclusao,
        item.Origens);
}
