using System.Text.Json;
using FluentValidation;

namespace InvoiSys.Api.Validacao;

public sealed class ValidacaoFiltro<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext contexto,
        EndpointFilterDelegate proximo)
    {
        var entrada = contexto.Arguments.OfType<T>().Single();
        var validator = contexto.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var resultado = await validator.ValidateAsync(entrada, contexto.HttpContext.RequestAborted);

        if (resultado.IsValid)
        {
            return await proximo(contexto);
        }

        var erros = resultado.Errors
            .GroupBy(erro => string.Join('.', erro.PropertyName.Split('.')
                .Select(JsonNamingPolicy.CamelCase.ConvertName)))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(erro => erro.ErrorMessage).ToArray());

        return Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Dados inválidos",
            detail: "Um ou mais campos são inválidos.",
            extensions: new Dictionary<string, object?> { ["errors"] = erros });
    }
}
