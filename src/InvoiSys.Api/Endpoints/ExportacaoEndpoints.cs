using InvoiSys.Api.Contracts;
using InvoiSys.Application.Exportacao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class ExportacaoEndpoints
{
    public static IEndpointRouteBuilder MapExportacaoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("exportacao");

        grupo.MapPost("/{chaveRelease}/exportar", ExportarAsync)
            .WithName("ExportarRelease")
            .WithSummary("Exporta o comunicado aprovado de uma Release para um público e formato.")
            .Produces<ComunicadoExportadoOut>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return rotas;
    }

    private static async Task<IResult> ExportarAsync(
        string chaveRelease,
        [FromBody] ExportarIn corpo,
        [FromServices] ExportacaoComunicado exportacao,
        CancellationToken cancellationToken)
    {
        if (!PublicoAlvoExtensions.TentarConverter(corpo.Publico, out var publico))
        {
            return Results.Problem(
                detail: $"Público inválido ou ausente: '{corpo.Publico}'. "
                    + "Use cliente, comercial, suporte ou interno.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (!FormatoExportacaoExtensions.TentarConverter(corpo.Formato, out var formato))
        {
            return Results.Problem(
                detail: $"Formato inválido ou ausente: '{corpo.Formato}'. "
                    + "Use markdown, html ou pdf.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        try
        {
            var exportado = await exportacao.ExportarAsync(
                chaveRelease,
                publico,
                formato,
                corpo.GeradoPor,
                cancellationToken);

            return Results.Created(
                $"/api/v1/releases/{chaveRelease}/exportar/{exportado.Id}",
                ComunicadoExportadoOut.De(exportado));
        }
        catch (Exception exc) when (MapearFalhaDeExportacao(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalhaDeExportacao(Exception exc) => exc switch
    {
        ReleaseNaoEncontradaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        ReleaseNaoAprovadaException or ReleaseSemItensProcessadosException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        FormatoNaoSuportadoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
