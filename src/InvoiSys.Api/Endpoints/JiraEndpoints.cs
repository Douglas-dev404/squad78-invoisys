using InvoiSys.Api.Contracts;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Api.Endpoints;

public static class JiraEndpoints
{
    public static IEndpointRouteBuilder MapJiraEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/jira/versoes", ListarVersoesAsync)
            .WithTags("jira")
            .WithName("ListarVersoesJira")
            .WithSummary("Lista as versões do projeto Jira configurado para seleção.")
            .RequireAuthorization()
            .Produces<IReadOnlyList<VersaoJiraOut>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return rotas;
    }

    private static async Task<IResult> ListarVersoesAsync(
        ConsultaVersoesJira consulta,
        CancellationToken cancellationToken)
    {
        try
        {
            var versoes = await consulta.ListarAsync(cancellationToken);
            return Results.Ok(versoes.Select(v => new VersaoJiraOut(
                v.Nome,
                v.Lancada,
                v.Arquivada,
                v.DataLancamento)).ToList());
        }
        catch (Exception exc) when (exc is ProviderNaoConfiguradoException or IntegracaoExternaException)
        {
            var status = exc is ProviderNaoConfiguradoException
                ? StatusCodes.Status501NotImplemented
                : StatusCodes.Status502BadGateway;
            return Results.Problem(detail: exc.Message, statusCode: status);
        }
    }
}
