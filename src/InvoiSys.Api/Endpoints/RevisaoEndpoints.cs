using InvoiSys.Api.Contracts;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

/// <summary>
/// Endpoints da revisão humana por público (aprovar, reprovar, reabrir). Camada fina:
/// converte o DTO, chama <see cref="RevisaoComunicado"/> e traduz o erro em HTTP.
/// </summary>
public static class RevisaoEndpoints
{
    public static IEndpointRouteBuilder MapRevisaoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("revisao");

        grupo.MapPost("/{chaveRelease}/aprovar", AprovarAsync)
            .WithName("AprovarRelease")
            .WithSummary("Aprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reprovar", ReprovarAsync)
            .WithName("ReprovarRelease")
            .WithSummary("Reprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reabrir", ReabrirAsync)
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
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            publico => revisao.AprovarAsync(
                chaveRelease,
                publico,
                corpo.AprovadoPor ?? string.Empty,
                cancellationToken));

    private static Task<IResult> ReprovarAsync(
        string chaveRelease,
        [FromBody] ReprovarIn corpo,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            publico => revisao.ReprovarAsync(
                chaveRelease,
                publico,
                corpo.RevisadoPor ?? string.Empty,
                corpo.Motivo ?? string.Empty,
                cancellationToken));

    private static Task<IResult> ReabrirAsync(
        string chaveRelease,
        [FromBody] ReabrirIn corpo,
        [FromServices] RevisaoComunicado revisao,
        CancellationToken cancellationToken) =>
        RevisarAsync(
            corpo.Publico,
            publico => revisao.ReabrirAsync(chaveRelease, publico, cancellationToken));

    /// <summary>
    /// Caminho comum das três rotas: converte o público do DTO, executa o caso de uso
    /// e traduz o resultado em HTTP — 204 no sucesso, ProblemDetails no erro.
    /// </summary>
    private static async Task<IResult> RevisarAsync(
        string? publicoInformado,
        Func<PublicoAlvo, Task> revisao)
    {
        if (!PublicoAlvoExtensions.TentarConverter(publicoInformado, out var publico))
        {
            return Results.Problem(
                detail: $"Público inválido ou ausente: '{publicoInformado}'. "
                    + "Use cliente, comercial, suporte ou interno.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        try
        {
            await revisao(publico);
            return Results.NoContent();
        }
        catch (Exception exc) when (MapearFalhaDeRevisao(exc) is { } problema)
        {
            return problema;
        }
    }

    /// <summary>
    /// Exceções do domínio → HTTP. Estado que impede a operação (sem comunicado gerado,
    /// já aprovada, transição inválida) é 409, igual ao /processar; entrada inválida
    /// (revisor ou motivo vazio) é 422. Qualquer outra segue como erro interno.
    /// </summary>
    private static IResult? MapearFalhaDeRevisao(Exception exc) => exc switch
    {
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
