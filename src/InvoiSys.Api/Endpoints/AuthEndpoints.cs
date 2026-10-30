using InvoiSys.Api.Auth;
using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validation;
using InvoiSys.Application.Acesso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiSys.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/auth").WithTags("auth");

        grupo.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<ValidacaoFiltro<LoginIn>>()
            .RequireRateLimiting(LimiteDeLogin.Politica)
            .WithName("Login")
            .WithSummary("Autentica com e-mail e senha e devolve um JWT.")
            .Produces<LoginOut>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithName("Me")
            .WithSummary("Dados do usuário autenticado.")
            .Produces<UsuarioOut>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPost("/logout", () => Results.NoContent())
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Sem efeito no servidor: o cliente descarta o token.")
            .Produces(StatusCodes.Status204NoContent);

        return rotas;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginIn corpo,
        [FromServices] Autenticacao autenticacao,
        CancellationToken cancellationToken)
    {
        var sessao = await autenticacao.AutenticarAsync(
            corpo.Email ?? string.Empty, corpo.Senha ?? string.Empty, cancellationToken);

        return sessao is null
            ? Results.Problem(
                detail: "E-mail ou senha inválidos.",
                statusCode: StatusCodes.Status401Unauthorized)
            : Results.Ok(new LoginOut(
                UsuarioOut.De(sessao.Usuario), sessao.Token.Token, sessao.Token.ExpiraEm));
    }

    private static async Task<IResult> MeAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        [FromServices] Autenticacao autenticacao,
        CancellationToken cancellationToken)
    {
        var usuario = Guid.TryParse(principal.FindFirst("sub")?.Value, out var id)
            ? await autenticacao.BuscarAtivoAsync(id, cancellationToken)
            : null;

        return usuario is null
            ? Results.Problem(
                detail: "Token inválido ou usuário inativo.",
                statusCode: StatusCodes.Status401Unauthorized)
            : Results.Ok(UsuarioOut.De(usuario));
    }
}