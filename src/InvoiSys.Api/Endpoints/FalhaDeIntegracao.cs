using InvoiSys.Domain.Ports;

namespace InvoiSys.Api.Endpoints;

internal static class FalhaDeIntegracao
{
    public static IResult? ParaProblema(Exception exc) => exc switch
    {
        ProviderNaoConfiguradoException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status501NotImplemented),
        IntegracaoExternaException =>
            Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status502BadGateway),
        _ => null,
    };
}
