using InvoiSys.Api.Contracts;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Endpoints;

public static class QuadroReleasesEndpoints
{
    public static IEndpointRouteBuilder MapQuadroReleasesEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/jira/releases", MontarAsync)
            .WithTags("releases")
            .WithName("QuadroReleases")
            .WithSummary("Releases (fixVersions) do projeto no Jira, cada uma com o status no InvoiSys, para o quadro.")
            .RequireAuthorization()
            .Produces<QuadroReleasesOut>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return rotas;
    }

    private static async Task<IResult> MontarAsync(QuadroReleases quadro, CancellationToken cancellationToken)
    {
        var resultado = await quadro.MontarAsync(cancellationToken);
        return Results.Ok(new QuadroReleasesOut(
            [.. resultado.Cartoes.Select(c => new CartaoReleaseOut(
                c.Chave,
                c.Descricao,
                c.NoJira,
                c.LancadaNoJira,
                c.DataLancamento,
                c.Status?.ParaValor() ?? "nao_processada",
                c.ProcessadaEm))],
            resultado.AvisoJira));
    }
}
