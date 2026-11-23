using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;
using InvoiSys.Application.Destinatarios;
using InvoiSys.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

public static class DestinatarioEndpoints
{
    public static IEndpointRouteBuilder MapDestinatarioEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/destinatarios").WithTags("destinatarios")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapGet("/", ListarAsync)
            .ComValidacao<ListarDestinatariosIn>()
            .WithName("ListarDestinatarios")
            .WithSummary("Destinatários de e-mail, filtrados por público; inativos só com incluirInativos=true.")
            .Produces<IReadOnlyList<DestinatarioOut>>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/", CadastrarAsync)
            .ComValidacao<SalvarDestinatarioIn>()
            .WithName("CadastrarDestinatario")
            .WithSummary("Cadastra um e-mail para receber os comunicados de um público.")
            .Produces<DestinatarioOut>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPut("/{id:guid}", AlterarAsync)
            .ComValidacao<SalvarDestinatarioIn>()
            .WithName("AlterarDestinatario")
            .WithSummary("Altera nome, e-mail ou público de um destinatário.")
            .Produces<DestinatarioOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapPost("/{id:guid}/desativar", DesativarAsync)
            .WithName("DesativarDestinatario")
            .WithSummary("Tira o destinatário da lista de envio; o histórico continua.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/{id:guid}/reativar", ReativarAsync)
            .WithName("ReativarDestinatario")
            .WithSummary("Volta o destinatário para a lista de envio.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return rotas;
    }

    private static async Task<IResult> ListarAsync(
        [AsParameters] ListarDestinatariosIn filtro,
        [FromServices] CadastroDestinatarios cadastro,
        CancellationToken cancellationToken)
    {
        PublicoAlvo? publico = PublicoAlvoExtensions.TentarConverter(filtro.Publico, out var convertido)
            ? convertido
            : null;

        var destinatarios = await cadastro.ListarAsync(publico, filtro.IncluirInativos ?? false, cancellationToken);
        return Results.Ok(destinatarios.Select(DestinatarioOut.De).ToList());
    }

    private static async Task<IResult> CadastrarAsync(
        [FromBody] SalvarDestinatarioIn entrada,
        [FromServices] CadastroDestinatarios cadastro,
        CancellationToken cancellationToken)
    {
        PublicoAlvoExtensions.TentarConverter(entrada.Publico, out var publico);

        try
        {
            // ComValidacao<SalvarDestinatarioIn> já recusou nome, e-mail e público inválidos com 422.
            var destinatario = await cadastro.CadastrarAsync(entrada.Nome!, entrada.Email!, publico, cancellationToken);
            return Results.Created($"/api/v1/destinatarios/{destinatario.Id}", DestinatarioOut.De(destinatario));
        }
        catch (Exception exc) when (MapearFalha(exc) is { } problema)
        {
            return problema;
        }
    }

    private static async Task<IResult> AlterarAsync(
        Guid id,
        [FromBody] SalvarDestinatarioIn entrada,
        [FromServices] CadastroDestinatarios cadastro,
        CancellationToken cancellationToken)
    {
        PublicoAlvoExtensions.TentarConverter(entrada.Publico, out var publico);

        try
        {
            var destinatario = await cadastro.AlterarAsync(id, entrada.Nome!, entrada.Email!, publico, cancellationToken);
            return Results.Ok(DestinatarioOut.De(destinatario));
        }
        catch (Exception exc) when (MapearFalha(exc) is { } problema)
        {
            return problema;
        }
    }

    private static async Task<IResult> DesativarAsync(
        Guid id,
        [FromServices] CadastroDestinatarios cadastro,
        CancellationToken cancellationToken)
    {
        try
        {
            await cadastro.DesativarAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exc) when (MapearFalha(exc) is { } problema)
        {
            return problema;
        }
    }

    private static async Task<IResult> ReativarAsync(
        Guid id,
        [FromServices] CadastroDestinatarios cadastro,
        CancellationToken cancellationToken)
    {
        try
        {
            await cadastro.ReativarAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exc) when (MapearFalha(exc) is { } problema)
        {
            return problema;
        }
    }

    private static IResult? MapearFalha(Exception exc) => exc switch
    {
        DestinatarioNaoEncontradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound),
        EmailDestinatarioDuplicadoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict),
        ArgumentException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => null,
    };
}
