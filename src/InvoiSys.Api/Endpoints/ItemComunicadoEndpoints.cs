using InvoiSys.Api.Contracts;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class ItemComunicadoEndpoints
{
    public static IEndpointRouteBuilder MapItemComunicadoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases/{chaveRelease}/itens/{itemId:guid}")
            .WithTags("itens");

        grupo.MapPatch("", EditarItemAsync)
            .WithName("EditarItem")
            .WithSummary("Troca o texto de um item pela versão revisada por um humano.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/excluir", ExcluirItemAsync)
            .WithName("ExcluirItem")
            .WithSummary("Tira o item do comunicado publicado, sem apagar o registro.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/reincluir", ReincluirItemAsync)
            .WithName("ReincluirItem")
            .WithSummary("Devolve ao comunicado um item excluído.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return rotas;
    }

    private static Task<IResult> EditarItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromBody] EditarItemIn corpo,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(() => revisao.EditarItemAsync(
            chaveRelease,
            itemId,
            corpo.Texto ?? string.Empty,
            cancellationToken));

    private static Task<IResult> ExcluirItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromBody] ExcluirItemIn? corpo,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(() => revisao.ExcluirItemAsync(
            chaveRelease,
            itemId,
            corpo?.Motivo,
            cancellationToken));

    private static Task<IResult> ReincluirItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(() => revisao.ReincluirItemAsync(chaveRelease, itemId, cancellationToken));

    private static async Task<IResult> RevisarItemAsync(Func<Task<ItemRevisado>> revisao)
    {
        try
        {
            var (item, publico) = await revisao();

            return Results.Ok(new ItemRevisaoOut(
                item.Id,
                publico.ParaValor(),
                item.Categoria.ParaValor(),
                item.Texto,
                item.TextoEditadoManualmente,
                item.TextoFinal,
                item.Incluido,
                item.MotivoExclusao,
                item.Origens));
        }
        catch (Exception exc) when (MapearFalhaDeRevisaoDeItem(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalhaDeRevisaoDeItem(Exception exc) => exc switch
    {
        ReleaseNaoEncontradaException or ItemNaoEncontradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        TransicaoDeStatusInvalidaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        ArgumentException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
