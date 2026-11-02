using System.Security.Claims;
using InvoiSys.Api.Autenticacao;
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
            .WithTags("itens")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

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
        ClaimsPrincipal usuario,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(usuario, revisorId => revisao.EditarItemAsync(
            chaveRelease,
            itemId,
            corpo.Texto ?? string.Empty,
            revisorId,
            cancellationToken));

    private static Task<IResult> ExcluirItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromBody] ExcluirItemIn? corpo,
        ClaimsPrincipal usuario,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(usuario, revisorId => revisao.ExcluirItemAsync(
            chaveRelease,
            itemId,
            corpo?.Motivo,
            revisorId,
            cancellationToken));

    private static Task<IResult> ReincluirItemAsync(
        string chaveRelease,
        Guid itemId,
        ClaimsPrincipal usuario,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarItemAsync(usuario, revisorId => revisao.ReincluirItemAsync(
            chaveRelease,
            itemId,
            revisorId,
            cancellationToken));

    private static async Task<IResult> RevisarItemAsync(
        ClaimsPrincipal usuario,
        Func<Guid, Task<ItemRevisado>> revisao)
    {
        if (!UsuarioAutenticado.TentarObterId(usuario, out var revisorId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var (item, publico) = await revisao(revisorId);

            return Results.Ok(ItemRevisaoOut.De(item, publico));
        }
        catch (Exception exc) when (MapearFalhaDeRevisaoDeItem(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalhaDeRevisaoDeItem(Exception exc) => exc switch
    {
        RevisorInvalidoException => Results.Unauthorized(),
        ReleaseNaoEncontradaException or ItemNaoEncontradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        TransicaoDeStatusInvalidaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        ArgumentException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
