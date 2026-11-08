using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiSys.Api.Auth;

public static class LimiteDeLogin
{
    public const string Politica = "login";
}

public static class ConfiguracaoHttp
{
    public static IServiceCollection AddCorsConfiguravel(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((cors, configuracao) =>
        {
            var origens = configuracao.GetSection("Cors:Origens").Get<string[]>() ?? [];
            cors.AddDefaultPolicy(politica =>
            {
                if (origens.Length > 0)
                {
                    politica.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddLimiteDeLogin(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((limitador, configuracao) =>
        {
            var tentativas = configuracao.GetValue("RateLimit:Login:Tentativas", 5);
            var janela = TimeSpan.FromSeconds(configuracao.GetValue("RateLimit:Login:JanelaSegundos", 60));

            limitador.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limitador.AddPolicy(LimiteDeLogin.Politica, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = tentativas,
                    Window = janela,
                    QueueLimit = 0,
                }));

            limitador.OnRejected = async (contexto, _) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    contexto.HttpContext.Response.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString();
                }

                await Results.Problem(
                    detail: "Muitas tentativas de login. Tente novamente em instantes.",
                    statusCode: StatusCodes.Status429TooManyRequests)
                    .ExecuteAsync(contexto.HttpContext);
            };
        });

        return services;
    }
}