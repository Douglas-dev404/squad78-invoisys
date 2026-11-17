using InvoiSys.Api.Contracts;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class JiraEndpoints
{
    public static IEndpointRouteBuilder MapJiraEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/jira").WithTags("jira")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapGet("/versoes", ListarVersoesAsync)
            .WithName("ListarVersoesJira")
            .WithSummary("Versões (fixVersions) do projeto no Jira, com o status da Release no InvoiSys. "
                + "Não lançadas primeiro; incluirLancadas=false traz só as não lançadas.")
            .Produces<IReadOnlyList<VersaoJiraOut>>()
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return rotas;
    }

    private static async Task<IResult> ListarVersoesAsync(
        [FromServices] ConsultaVersoesJira consulta,
        CancellationToken cancellationToken,
        [FromQuery] bool incluirLancadas = true)
    {
        try
        {
            var versoes = await consulta.ListarAsync(incluirLancadas, cancellationToken);

            return Results.Ok(versoes.Select(v => new VersaoJiraOut(
                v.Versao.Nome,
                v.Versao.Descricao,
                v.Versao.DataLancamento,
                v.Versao.Lancada,
                v.StatusInvoisys?.ParaValor())).ToList());
        }
        catch (ProviderNaoConfiguradoException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status501NotImplemented);
        }
        catch (IntegracaoExternaException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
