using InvoiSys.Api.Contracts;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
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

        grupo.MapPost("/{chaveRelease}/aprovar", AprovarReleaseAsync)
            .WithName("AprovarRelease")
            .WithSummary("Aprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reprovar", ReprovarReleaseAsync)
            .WithName("ReprovarRelease")
            .WithSummary("Reprova o comunicado de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{chaveRelease}/reabrir", ReabrirReleaseAsync)
            .WithName("ReabrirRelease")
            .WithSummary("Reabre a revisão de uma Release para um público.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

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
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return rotas;
    }

    /// <summary>
    /// Busca as histórias de uma Release direto do Jira, sem passar pelo pipeline de
    /// IA — útil pra conferir o que vai entrar no processamento antes de gastar tokens.
    /// </summary>


    private static bool TryParsePublico(string? valor, out PublicoAlvo publico)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            publico = PublicoAlvo.Cliente;
            return true;
        }

        foreach (var candidato in Enum.GetValues<PublicoAlvo>())
        {
            if (string.Equals(
                    candidato.ParaValor(),
                    valor,
                    StringComparison.OrdinalIgnoreCase))
            {
                publico = candidato;
                return true;
            }
        }

        publico = default;
        return false;
    }

    private static async Task<IResult> AprovarReleaseAsync(
    string chaveRelease,
    AprovarReleaseRequest request,
    [FromServices] IReleaseRepository repositorio,
    CancellationToken cancellationToken)
    {
        if (!TryParsePublico(request.Publico, out var publico))
        {
            return Results.UnprocessableEntity(new
            {
                detail = $"Público inválido: '{request.Publico}'."
            });
        }

        var release = await repositorio.BuscarPorChaveJiraAsync(
            chaveRelease,
            cancellationToken);

        if (release is null)
        {
            return Results.NotFound(new
            {
                detail = $"Release '{chaveRelease}' não encontrada."
            });
        }

        try
        {
            release.Aprovar(
                request.AprovadoPor,
                DateTimeOffset.UtcNow,
                publico);

            await repositorio.SalvarAsync(
                release,
                cancellationToken);

            return Results.NoContent();
        }
        catch (ReleaseSemItensProcessadosException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (RevisaoHumanaObrigatoriaException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (VersaoSemItensException exc)
        {
            return Results.UnprocessableEntity(new
            {
                detail = exc.Message
            });
        }
        catch (TransicaoDeStatusInvalidaException exc)
        {
            return Results.UnprocessableEntity(new
            {
                detail = exc.Message
            });
        }
    }


    private static async Task<IResult> ReprovarReleaseAsync(
        string chaveRelease,
        ReprovarReleaseRequest request,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken)
    {
        if (!TryParsePublico(request.Publico, out var publico))
        {
            return Results.UnprocessableEntity(new
            {
                detail = $"Público inválido: '{request.Publico}'."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            return Results.UnprocessableEntity(new
            {
                detail = "O motivo da reprovação é obrigatório."
            });
        }

        var release = await repositorio.BuscarPorChaveJiraAsync(
            chaveRelease,
            cancellationToken);

        if (release is null)
        {
            return Results.NotFound(new
            {
                detail = $"Release '{chaveRelease}' não encontrada."
            });
        }

        try
        {
            release.Reprovar(
                request.RevisadoPor,
                request.Motivo,
                DateTimeOffset.UtcNow,
                publico);

            await repositorio.SalvarAsync(
                release,
                cancellationToken);

            return Results.NoContent();
        }
        catch (ReleaseSemItensProcessadosException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (RevisaoHumanaObrigatoriaException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (ArgumentException exc)
        {
            return Results.UnprocessableEntity(new
            {
                detail = exc.Message
            });
        }
        catch (TransicaoDeStatusInvalidaException exc)
        {
            return Results.UnprocessableEntity(new
            {
                detail = exc.Message
            });
        }
    }

    private static async Task<IResult> ReabrirReleaseAsync(
        string chaveRelease,
        ReabrirReleaseRequest request,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken)
    {
        if (!TryParsePublico(request.Publico, out var publico))
        {
            return Results.UnprocessableEntity(new
            {
                detail = $"Público inválido: '{request.Publico}'."
            });
        }

        var release = await repositorio.BuscarPorChaveJiraAsync(
            chaveRelease,
            cancellationToken);

        if (release is null)
        {
            return Results.NotFound(new
            {
                detail = $"Release '{chaveRelease}' não encontrada."
            });
        }

        try
        {
            release.Reabrir(publico);

            await repositorio.SalvarAsync(
                release,
                cancellationToken);

            return Results.NoContent();
        }
        catch (ReleaseSemItensProcessadosException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (RevisaoHumanaObrigatoriaException exc)
        {
            return Results.Conflict(new
            {
                detail = exc.Message
            });
        }
        catch (TransicaoDeStatusInvalidaException exc)
        {
            return Results.UnprocessableEntity(new
            {
                detail = exc.Message
            });
        }
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
    ///
    /// Versão já aprovada devolve 409: o estado atual do recurso impede a operação, e o
    /// caminho é reabrir a revisão antes de reprocessar.
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
        catch (RevisaoHumanaObrigatoriaException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict);
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
