using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Integracoes;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class IntegracaoEndpoints
{
    public static IEndpointRouteBuilder MapIntegracaoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var admin = rotas.MapGroup("/api/v1/integracoes").WithTags("integracoes").RequireAuthorization("Admin");
        admin.MapGet("/", Obter).Produces<IntegracoesOut>();
        admin.MapPut("/jira", SalvarJiraAsync).ComValidacao<SalvarJiraIn>()
            .Produces<IntegracoesOut>().ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        admin.MapPut("/ia", SalvarIaAsync).ComValidacao<SalvarIaIn>()
            .Produces<IntegracoesOut>().ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        admin.MapPost("/jira/testar", TestarJiraAsync).Produces<ResultadoTesteOut>();
        admin.MapPost("/ia/testar", TestarIaAsync).Produces<ResultadoTesteOut>();

        var execucoes = rotas.MapGroup("/api/v1/execucoes").WithTags("execucoes").RequireAuthorization();
        execucoes.MapGet("/ao-vivo", AoVivo).Produces<ExecucoesAoVivoOut>();

        return rotas;
    }

    private static IResult Obter([FromServices] AdministracaoIntegracoes administracao) =>
        Results.Ok(ParaOut(administracao.Obter()));

    private static async Task<IResult> SalvarJiraAsync(
        [FromBody] SalvarJiraIn entrada,
        [FromServices] AdministracaoIntegracoes administracao,
        CancellationToken cancellationToken)
    {
        await administracao.SalvarJiraAsync(
            entrada.BaseUrl, entrada.Email, entrada.Projeto, entrada.ApiToken, cancellationToken);
        return Results.Ok(ParaOut(administracao.Obter()));
    }

    private static async Task<IResult> SalvarIaAsync(
        [FromBody] SalvarIaIn entrada,
        [FromServices] AdministracaoIntegracoes administracao,
        CancellationToken cancellationToken)
    {
        await administracao.SalvarIaAsync(entrada.Provedor!, entrada.Modelo, entrada.ApiKey, cancellationToken);
        return Results.Ok(ParaOut(administracao.Obter()));
    }

    private static async Task<IResult> TestarJiraAsync(
        [FromServices] AdministracaoIntegracoes administracao, CancellationToken cancellationToken)
    {
        var r = await administracao.TestarJiraAsync(cancellationToken);
        return Results.Ok(new ResultadoTesteOut(r.Ok, r.Mensagem, r.DuracaoMs));
    }

    private static async Task<IResult> TestarIaAsync(
        [FromServices] AdministracaoIntegracoes administracao, CancellationToken cancellationToken)
    {
        var r = await administracao.TestarIaAsync(cancellationToken);
        return Results.Ok(new ResultadoTesteOut(r.Ok, r.Mensagem, r.DuracaoMs));
    }

    private static IResult AoVivo(
        [FromServices] IMonitorExecucao monitor,
        [FromQuery] long depois = 0,
        [FromQuery] string? release = null)
    {
        var eventos = monitor.Depois(depois, release);
        var ultimo = eventos.Count > 0 ? eventos[^1].Id : depois;
        return Results.Ok(new ExecucoesAoVivoOut(
            ultimo,
            [.. eventos.Select(e => new EventoExecucaoOut(
                e.Id, e.Em, e.Release, e.Tipo, e.Mensagem, e.Prompt, e.Resposta, e.Modelo, e.DuracaoMs))]));
    }

    private static IntegracoesOut ParaOut(IntegracoesVisao v) => new(
        new IntegracaoJiraOut(
            v.Jira.BaseUrl, v.Jira.Email, v.Jira.Projeto, v.Jira.TokenConfigurado, v.Jira.TokenDoSistema, v.Jira.Configurado),
        new IntegracaoIaOut(
            v.Ia.Provedor,
            new IntegracaoProvedorIaOut(v.Ia.OpenAi.Modelo, v.Ia.OpenAi.ChaveConfigurada, v.Ia.OpenAi.ChaveDoSistema),
            new IntegracaoProvedorIaOut(v.Ia.OpenRouter.Modelo, v.Ia.OpenRouter.ChaveConfigurada, v.Ia.OpenRouter.ChaveDoSistema),
            v.Ia.Configurado));
}
