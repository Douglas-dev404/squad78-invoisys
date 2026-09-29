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

        // Os Produces abaixo não são decoração: sem eles o gerador de OpenAPI só
        // enxerga o IResult do handler e publica um spec com "200 OK" e nenhum schema,
        // inútil para quem for consumir a API.
        grupo.MapGet("/", ListarReleasesAsync)
            .WithName("ListarReleases")
            .WithSummary("Lista Releases já persistidas, opcionalmente filtrando por status.")
            .Produces<IReadOnlyList<ReleaseResumoOut>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapGet("/{chaveRelease}", BuscarReleaseAsync)
            .WithName("BuscarRelease")
            .WithSummary("Consulta uma Release persistida, com todas as versões por público.")
            .Produces<ReleaseDetalheOut>()
            .ProducesProblem(StatusCodes.Status404NotFound);

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
    /// Lista Releases já persistidas — a fila de revisão da tela do frontend usa isto
    /// com <c>?status=aguardando_revisao</c>. Sem filtro, lista tudo.
    /// </summary>
    private static async Task<IResult> ListarReleasesAsync(
        string? status,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken)
    {
        StatusPipeline? statusFiltro = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!StatusPipelineExtensions.TentarConverter(status, out var valor))
            {
                return Results.Problem(
                    detail: $"Status '{status}' inválido. Valores aceitos: "
                        + string.Join(", ", Enum.GetValues<StatusPipeline>().Select(s => s.ParaValor())),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            statusFiltro = valor;
        }

        var releases = await repositorio.ListarAsync(statusFiltro, cancellationToken);

        return Results.Ok(releases
            .Select(r => new ReleaseResumoOut(r.ChaveJira, r.Status.ParaValor()))
            .ToList());
    }

    /// <summary>
    /// Consulta uma Release persistida pela chave do Jira, com todas as versões por
    /// público-alvo (não só o atalho Cliente) — o que a tela de revisão precisa pra
    /// abrir uma Release específica.
    /// </summary>
    private static async Task<IResult> BuscarReleaseAsync(
        string chaveRelease,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken)
    {
        var release = await repositorio.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken);

        if (release is null)
        {
            return Results.Problem(
                detail: $"Release '{chaveRelease}' não encontrada.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(new ReleaseDetalheOut(
            ChaveJira: release.ChaveJira,
            Status: release.Status.ParaValor(),
            Historias: [.. release.Historias.Select(h => new HistoriaJiraOut(
                h.Chave,
                h.Titulo,
                h.TipoIssue,
                h.PossuiReleaseNoteDedicada))],
            Versoes: [.. release.Versoes.Select(v => new VersaoComunicadoOut(
                v.Publico.ParaValor(),
                v.Status.ParaValor(),
                v.TituloExecutivo,
                v.ResumoExecutivo,
                [.. v.Itens.Select(i => new ItemComunicadoDetalheOut(
                    i.Categoria.ParaValor(),
                    i.TextoFinal,
                    i.Origens,
                    i.Incluido,
                    i.MotivoExclusao))],
                v.RevisadoPor,
                v.RevisadoEm,
                v.MotivoReprovacao))],
            Execucoes: [.. release.Execucoes.Select(e => new ExecucaoPipelineOut(
                e.Status.ParaValor(),
                e.ModeloLlm,
                e.Erro,
                e.IniciadoEm,
                e.ConcluidoEm))]));
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
