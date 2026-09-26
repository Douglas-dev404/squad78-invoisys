using InvoiSys.Api.Contracts;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
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
        catch (Exception exc) when (MapearFalhaDeIntegracao(exc) is { } problema)
        {
            return problema;
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
        catch (Exception exc) when (MapearFalhaDeIntegracao(exc) is { } problema)
        {
            return problema;
        }
    }

    /// <summary>
    /// Traduz o contrato de erro das portas (InvoiSys.Domain.Ports) em HTTP. Pega pelo
    /// tipo do domínio, nunca pelo do adapter: a API não sabe se o Jira/LLM por trás é o
    /// real, o pendente ou um fake de teste.
    ///
    /// Falha de dependência externa (Jira/LLM fora do ar, resposta fora do formato) é
    /// 502 com a causa no corpo, não 500 mudo; integração sem credencial é 501. Qualquer
    /// outra exceção devolve null e segue como erro interno de verdade.
    /// </summary>
    private static IResult? MapearFalhaDeIntegracao(Exception exc) => exc switch
    {
        ProviderNaoConfiguradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status501NotImplemented),
        IntegracaoExternaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway),
        _ => null,
    };
}
