using System.Security.Claims;
using InvoiSys.Api.Auth;
using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;
using InvoiSys.Application.Consulta;
using InvoiSys.Application.Exportacao;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class ExportacaoEndpoints
{
    public static IEndpointRouteBuilder MapExportacaoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("exportacao")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPost("/{chaveRelease}/exportar", ExportarAsync)
            .WithName("ExportarRelease")
            .WithSummary("Exporta o comunicado aprovado de uma Release para um público e formato.")
            .Produces<ComunicadoExportadoOut>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        var consulta = rotas.MapGroup("/api/v1/exportacoes").WithTags("exportacao")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        consulta.MapGet("/", ListarAsync)
            .ComValidacao<ListarExportacoesIn>()
            .WithName("ListarExportacoes")
            .WithSummary("Histórico de exportações, mais recente primeiro, com filtro de público e busca por chave ou título.")
            .Produces<PaginaOut<ExportacaoResumoOut>>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        consulta.MapGet("/{id:guid}", BuscarAsync)
            .WithName("BuscarExportacao")
            .WithSummary("Uma exportação com o conteúdo gerado.")
            .Produces<ExportacaoDetalheOut>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return rotas;
    }

    private static async Task<IResult> ListarAsync(
        [AsParameters] ListarExportacoesIn filtro,
        [FromServices] ConsultaExportacoes consulta,
        CancellationToken cancellationToken)
    {
        var pagina = filtro.Pagina ?? ListarExportacoesIn.PaginaPadrao;
        var tamanho = filtro.Tamanho ?? ListarExportacoesIn.TamanhoPadrao;
        PublicoAlvo? publico = PublicoAlvoExtensions.TentarConverter(filtro.Publico, out var convertido)
            ? convertido
            : null;

        var resultado = await consulta.ListarAsync(publico, filtro.Busca, pagina, tamanho, cancellationToken);

        return Results.Ok(new PaginaOut<ExportacaoResumoOut>(
            [.. resultado.Itens.Select(ExportacaoResumoOut.De)],
            resultado.Total,
            pagina,
            tamanho));
    }

    private static async Task<IResult> BuscarAsync(
        Guid id,
        [FromServices] ConsultaExportacoes consulta,
        CancellationToken cancellationToken) =>
        await consulta.BuscarAsync(id, cancellationToken) is { } detalhe
            ? Results.Ok(ExportacaoDetalheOut.De(detalhe))
            : Results.Problem(detail: $"Exportação {id} não encontrada.", statusCode: StatusCodes.Status404NotFound);

    private static async Task<IResult> ExportarAsync(
        string chaveRelease,
        [FromBody] ExportarIn corpo,
        ClaimsPrincipal usuario,
        [FromServices] ExportacaoComunicado exportacao,
        CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.TentarObterId(usuario, out var usuarioId))
        {
            return Results.Unauthorized();
        }

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
                usuarioId,
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
        RevisorInvalidoException => Results.Unauthorized(),
        ReleaseNaoEncontradaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        ReleaseNaoAprovadaException or ReleaseSemItensProcessadosException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        FormatoNaoSuportadoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
