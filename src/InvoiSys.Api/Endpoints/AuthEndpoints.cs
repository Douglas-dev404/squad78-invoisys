using InvoiSys.Api.Auth;
using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;
using InvoiSys.Application.Acesso;
using InvoiSys.Application.Autenticacao;
using InvoiSys.Domain.Entities;
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
            .ComValidacao<LoginIn>()
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

        grupo.MapPost("/forgot-password", EsqueciSenhaAsync)
            .AllowAnonymous()
            .WithName("EsqueciSenha")
            .WithSummary("Envia o link de redefinição de senha, sem revelar se o e-mail existe.")
            .RequireRateLimiting(LimiteDeLogin.PoliticaEsqueciSenha)
            .Produces<EsqueciSenhaOut>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/resend-forgot-password", ReenviarEsqueciSenhaAsync)
            .AllowAnonymous()
            .WithName("ReenviarEsqueciSenha")
            .WithSummary("Reenvia o link de redefinição e invalida o token anterior.")
            .RequireRateLimiting(LimiteDeLogin.PoliticaEsqueciSenha)
            .Produces<EsqueciSenhaOut>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/reset-password", RedefinirSenhaAsync)
            .AllowAnonymous()
            .WithName("RedefinirSenha")
            .WithSummary("Troca a senha usando o token recebido por e-mail (uso único, 30 minutos).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

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

    private static Task<IResult> EsqueciSenhaAsync(
        [FromBody] EsqueciSenhaIn corpo,
        [FromServices] RecuperacaoSenha recuperacao,
        CancellationToken cancellationToken) =>
        EnviarAsync(() => recuperacao.SolicitarAsync(corpo.Email, cancellationToken));

    private static Task<IResult> ReenviarEsqueciSenhaAsync(
        [FromBody] EsqueciSenhaIn corpo,
        [FromServices] RecuperacaoSenha recuperacao,
        CancellationToken cancellationToken) =>
        EnviarAsync(() => recuperacao.ReenviarAsync(corpo.Email, cancellationToken));

    private static async Task<IResult> RedefinirSenhaAsync(
        [FromBody] RedefinirSenhaIn corpo,
        [FromServices] RecuperacaoSenha recuperacao,
        CancellationToken cancellationToken)
    {
        try
        {
            await recuperacao.RedefinirAsync(corpo.Token, corpo.NovaSenha, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exc) when (exc is ArgumentException or TokenRedefinicaoInvalidoException)
        {
            return Results.Problem(
                detail: exc is ArgumentException arg ? SemNomeDoParametro(arg) : exc.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    private static async Task<IResult> EnviarAsync(Func<Task> acao)
    {
        try
        {
            await acao();
            return Results.Ok(new EsqueciSenhaOut(
                RecuperacaoSenha.MensagemEnvio,
                TokenRedefinicaoSenha.ValidadeMinutos));
        }
        catch (ArgumentException exc)
        {
            return Results.Problem(
                detail: SemNomeDoParametro(exc),
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    private static string SemNomeDoParametro(ArgumentException exc) =>
        exc.Message.Split(" (Parameter", 2)[0];
}
