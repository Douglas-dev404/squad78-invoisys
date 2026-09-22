using InvoiSys.Api.Contracts;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Infrastructure.Llm;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

/// <summary>
/// Endpoints REST de Release. Camada fina: só traduz HTTP em chamada de serviço,
/// nenhuma regra de negócio mora aqui.
/// </summary>
public static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases").WithTags("releases");

        // Os Produces abaixo não são decoração: sem eles o gerador de OpenAPI só
        // enxerga o IResult do handler e publica um spec com "200 OK" e nenhum schema,
        // inútil para quem for consumir a API.
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
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return rotas;
    }

    /// <summary>
    /// Busca as histórias de uma Release direto do Jira, sem passar pelo pipeline de
    /// IA — útil pra conferir o que vai entrar no processamento antes de gastar tokens.
    /// </summary>
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
                // Este endpoint só busca no Jira, nunca roda o pipeline — status é
                // sempre pendente aqui.
                Status: StatusPipeline.Pendente.ParaValor(),
                TotalHistorias: historias.Count,
                Historias: [.. historias.Select(h => new HistoriaJiraOut(
                    h.Chave,
                    h.Titulo,
                    h.TipoIssue,
                    h.PossuiReleaseNoteDedicada))]));
        }
        catch (JiraApiException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (ProviderNaoConfiguradoException exc)
        {
            return Results.Problem(
                detail: exc.Message,
                statusCode: StatusCodes.Status501NotImplemented);
        }
    }

    /// <summary>
    /// Roda o pipeline de IA completo sobre a Release. Resultado fica em
    /// aguardando_revisao — nunca publicado direto (ver Release.Aprovar).
    /// </summary>
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
        catch (JiraApiException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (ProviderNaoConfiguradoException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status501NotImplemented);
        }
        catch (Exception exc) when (exc is LlmRespostaInvalidaException or OpenRouterApiException)
        {
            // Falha do serviço de IA (fora do ar, ou resposta fora do formato) é falha
            // de dependência externa, não erro interno nosso: 502 com a causa no corpo,
            // em vez de 500 mudo que não diz nada a quem chamou.
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
