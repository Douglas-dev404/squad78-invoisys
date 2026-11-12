using System.Security.Claims;
using InvoiSys.Api.Auth;
using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class RevisaoEndpoints
{
    public static IEndpointRouteBuilder MapRevisaoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("revisao")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPost("/{chaveRelease}/aprovar", AprovarAsync)
            .ComValidacao<AprovarIn>()
            .WithName("AprovarRelease")
            .WithSummary("Aprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reprovar", ReprovarAsync)
            .ComValidacao<ReprovarIn>()
            .WithName("ReprovarRelease")
            .WithSummary("Reprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reabrir", ReabrirAsync)
            .ComValidacao<ReabrirIn>()
            .WithName("ReabrirRelease")
            .WithSummary("Reabre a revisão de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return rotas;
    }

    private static Task<IResult> AprovarAsync(
        string chaveRelease,
        [FromBody] AprovarIn corpo,
        ClaimsPrincipal usuario,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            usuario,
            (publico, revisorId) => revisao.AprovarAsync(
                chaveRelease,
                publico,
                revisorId,
                cancellationToken));

    private static Task<IResult> ReprovarAsync(
        string chaveRelease,
        [FromBody] ReprovarIn corpo,
        ClaimsPrincipal usuario,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            usuario,
            (publico, revisorId) => revisao.ReprovarAsync(
                chaveRelease,
                publico,
                revisorId,
                corpo.Motivo ?? string.Empty,
                cancellationToken));

    private static Task<IResult> ReabrirAsync(
        string chaveRelease,
        [FromBody] ReabrirIn corpo,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            null,
            (publico, _) => revisao.ReabrirAsync(chaveRelease, publico, cancellationToken));

    private static async Task<IResult> RevisarAsync(
        string? publicoInformado,
        ClaimsPrincipal? usuario,
        Func<PublicoAlvo, Guid, Task> revisao)
    {
        var revisorId = Guid.Empty;
        if (usuario is not null && !UsuarioAutenticado.TentarObterId(usuario, out revisorId))
        {
            return Results.Unauthorized();
        }

        PublicoAlvoExtensions.TentarConverter(publicoInformado, out var publico);

        try
        {
            await revisao(publico, revisorId);
            return Results.NoContent();
        }
        catch (Exception exc) when (MapearFalhaDeRevisao(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalhaDeRevisao(Exception exc) => exc switch
    {
        RevisorInvalidoException => Results.Unauthorized(),
        ReleaseNaoEncontradaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        ReleaseSemItensProcessadosException
            or RevisaoHumanaObrigatoriaException
            or TransicaoDeStatusInvalidaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        ArgumentException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
