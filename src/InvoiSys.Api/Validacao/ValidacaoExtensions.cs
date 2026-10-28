using FluentValidation;

namespace InvoiSys.Api.Validacao;

public static class ValidacaoExtensions
{
    public static IServiceCollection AddValidacaoDeEntrada(this IServiceCollection services) =>
        services.AddValidatorsFromAssemblyContaining<AprovarInValidator>();

    public static RouteHandlerBuilder ComValidacao<T>(this RouteHandlerBuilder rota) where T : class =>
        rota.AddEndpointFilter<ValidacaoFiltro<T>>();
}
