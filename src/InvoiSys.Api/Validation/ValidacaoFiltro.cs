using FluentValidation;

namespace InvoiSys.Api.Validation;

public sealed class ValidacaoFiltro<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext contexto, EndpointFilterDelegate proximo)
    {
        var corpo = contexto.Arguments.OfType<T>().FirstOrDefault();
        if (corpo is null)
        {
            return Results.Problem(
                detail: "Corpo da requisição ausente.",
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var validador = contexto.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var resultado = await validador.ValidateAsync(corpo, contexto.HttpContext.RequestAborted);

        return resultado.IsValid
            ? await proximo(contexto)
            : Results.ValidationProblem(
                resultado.ToDictionary(),
                statusCode: StatusCodes.Status422UnprocessableEntity);
    }
}