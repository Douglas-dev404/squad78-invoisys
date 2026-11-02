using System.Security.Claims;
using InvoiSys.Api.Contracts;
using InvoiSys.Application.Usuarios;
using InvoiSys.Domain.Entities;
using InvoiSys.Api.Validacao;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/usuarios").WithTags("usuarios").RequireAuthorization("Admin");
        grupo.MapPost("/", CadastrarAsync).ComValidacao<CadastrarUsuarioIn>().Produces<UsuarioOut>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict).ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        grupo.MapGet("/", ListarAsync).Produces<IReadOnlyList<UsuarioOut>>();
        grupo.MapPost("/{id:guid}/desativar", DesativarAsync).Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        return rotas;
    }

    private static async Task<IResult> CadastrarAsync(
        [FromBody] CadastrarUsuarioIn entrada,
        [FromServices] AdministracaoUsuarios administracao,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuario = await administracao.CadastrarAsync(entrada.Nome, entrada.Email, entrada.Papel,
                entrada.SenhaInicial, cancellationToken);
            return Results.Created($"/api/v1/usuarios/{usuario.Id}", ParaOut(usuario));
        }
        catch (EmailUsuarioDuplicadoException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: 409);
        }
        catch (Exception exc) when (exc is PapelUsuarioInvalidoException or SenhaInicialInvalidaException)
        {
            return Results.Problem(detail: exc.Message, statusCode: 422);
        }
    }

    private static async Task<IResult> ListarAsync(
        [FromServices] AdministracaoUsuarios administracao, CancellationToken cancellationToken) =>
        Results.Ok((await administracao.ListarAsync(cancellationToken)).Select(ParaOut).ToList());

    private static async Task<IResult> DesativarAsync(
        Guid id, ClaimsPrincipal principal,
        [FromServices] AdministracaoUsuarios administracao, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var administradorId))
            return Results.Forbid();
        try
        {
            await administracao.DesativarAsync(id, administradorId, cancellationToken);
            return Results.NoContent();
        }
        catch (UsuarioNaoEncontradoException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: 404);
        }
        catch (AutoDesativacaoUsuarioException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: 422);
        }
    }

    private static UsuarioOut ParaOut(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.Papel, usuario.Ativo, usuario.CriadoEm);
}
