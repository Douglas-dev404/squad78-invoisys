using InvoiSys.Api.Contracts;
using InvoiSys.Application.Autenticacao;
using InvoiSys.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class AuthEndpoints
{
    public const string PoliticaLimiteEnvio = "esqueci-senha";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/auth").WithTags("auth").AllowAnonymous();

        grupo.MapPost("/forgot-password", EsqueciSenhaAsync)
            .WithName("EsqueciSenha")
            .WithSummary("Envia o link de redefinição de senha, sem revelar se o e-mail existe.")
            .RequireRateLimiting(PoliticaLimiteEnvio)
            .Produces<EsqueciSenhaOut>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/resend-forgot-password", ReenviarEsqueciSenhaAsync)
            .WithName("ReenviarEsqueciSenha")
            .WithSummary("Reenvia o link de redefinição e invalida o token anterior.")
            .RequireRateLimiting(PoliticaLimiteEnvio)
            .Produces<EsqueciSenhaOut>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/reset-password", RedefinirSenhaAsync)
            .WithName("RedefinirSenha")
            .WithSummary("Troca a senha usando o token recebido por e-mail (uso único, 30 minutos).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return rotas;
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
