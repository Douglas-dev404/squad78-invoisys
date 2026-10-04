using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Contracts;

/// <summary>Corpo de <c>PATCH .../itens/{itemId}</c>: o texto revisado por um humano.</summary>
public sealed record EditarItemIn(string? Texto);

/// <summary>Corpo opcional de <c>POST .../itens/{itemId}/excluir</c>.</summary>
public sealed record ExcluirItemIn(string? Motivo);

/// <summary>
/// Estado de revisão de um item. Traz o <c>Id</c> (alvo das rotas de item), o texto da IA
/// e a edição humana lado a lado, o <c>TextoFinal</c> que vai para o comunicado e se o
/// item está incluído. É o mesmo formato no detalhe da Release e na resposta das rotas de
/// item: a tela monta a linha com o detalhe e a atualiza com a resposta da ação, sem
/// buscar a Release inteira de novo.
/// </summary>
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
