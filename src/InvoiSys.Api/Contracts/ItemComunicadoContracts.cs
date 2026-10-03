namespace InvoiSys.Api.Contracts;

/// <summary>Corpo de <c>PATCH .../itens/{itemId}</c>: o texto revisado por um humano.</summary>
public sealed record EditarItemIn(string? Texto);

/// <summary>Corpo opcional de <c>POST .../itens/{itemId}/excluir</c>.</summary>
public sealed record ExcluirItemIn(string? Motivo);

/// <summary>
/// Estado de um item depois de uma ação de revisão. Traz o <c>Id</c> (alvo das rotas de
/// item), o texto da IA e a edição humana lado a lado, o <c>TextoFinal</c> que vai para o
/// comunicado e se o item está incluído — o bastante para a tela atualizar a linha sem
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
    IReadOnlyList<string> Origens);
