using InvoiSys.Api.Contracts;
using InvoiSys.Application.Consulta;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class BrandingEndpoints
{
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/branding/highlights", ListarDestaquesAsync)
            .AllowAnonymous()
            .WithTags("branding")
            .WithName("ListarDestaques")
            .WithSummary("Lista os destaques ativos da tela de login na ordem de exibição.")
            .Produces<IReadOnlyList<DestaqueOut>>();

        return rotas;
    }

    private static async Task<IResult> ListarDestaquesAsync(
        [FromServices] ConsultaDestaques consulta,
        CancellationToken cancellationToken)
    {
        var destaques = await consulta.ListarAsync(cancellationToken);

        return Results.Ok(destaques
            .Select(d => new DestaqueOut(d.Id, d.Titulo, d.Descricao, d.Icone))
            .ToList());
    }
}
