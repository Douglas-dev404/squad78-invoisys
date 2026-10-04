using InvoiSys.Api.Contracts;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("releases");

        // Sem .Produces/.ProducesProblem o OpenAPI publica "200" sem schema (OpenApiTests cobre).
        grupo.MapGet("/{chaveRelease}/historias", BuscarHistoriasAsync)
            .WithName("BuscarHistorias")
            .WithSummary("Busca as histórias de uma Release direto do Jira, sem rodar a IA.")
            .Produces<ReleaseOut>()
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        grupo.MapPost("/{chaveRelease}/processar", ProcessarReleaseAsync)
            .WithName("ProcessarRelease")
            .WithSummary("Roda o pipeline de IA completo sobre a Release.")
            .Produces<ReleaseProcessadaOut>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return rotas;
    }

    private static async Task<IResult> BuscarHistoriasAsync(
        string chaveRelease,
        [FromServices] IJiraClient jiraClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var historias = await jiraClient.BuscarHistoriasDaReleaseAsync(
                chaveRelease,
                cancellationToken);

            return Results.Ok(new ReleaseOut(
                ChaveJira: chaveRelease,
                Status: StatusPipeline.Pendente.ParaValor(),
                TotalHistorias: historias.Count,
                Historias: [.. historias.Select(h => new HistoriaJiraOut(
                    h.Chave,
                    h.Titulo,
                    h.TipoIssue,
                    h.PossuiReleaseNoteDedicada))]));
        }
        catch (Exception exc) when (MapearFalhaDeIntegracao(exc) is { } problema)
        {
            return problema;
        }
    }

    private static async Task<IResult> ProcessarReleaseAsync(
        string chaveRelease,
        [FromServices] PipelineGeracaoReleaseNote pipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            var release = await pipeline.ExecutarAsync(chaveRelease, cancellationToken);

            return Results.Ok(new ReleaseProcessadaOut(
                ChaveJira: release.ChaveJira,
                Status: release.Status.ParaValor(),
                TituloExecutivo: release.TituloExecutivo,
                ResumoExecutivo: release.ResumoExecutivo,
                Itens: [.. release.Itens.Select(i => new ItemComunicadoOut(
                    i.Categoria.ParaValor(),
                    i.Texto,
                    i.Origens))]));
        }
        catch (RevisaoHumanaObrigatoriaException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (Exception exc) when (MapearFalhaDeIntegracao(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalhaDeIntegracao(Exception exc) => exc switch
    {
        ProviderNaoConfiguradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status501NotImplemented),
        IntegracaoExternaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway),
        _ => null,
    };
}
