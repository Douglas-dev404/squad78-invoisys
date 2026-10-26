using InvoiSys.Api.Endpoints;
using InvoiSys.Application;
using System.Text;
using System.Threading.RateLimiting;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

if (args.Contains("--healthcheck"))
{
    return await ExecutarHealthCheckAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

var auth = builder.Configuration.GetSection(AuthOptions.SecaoConfig).Get<AuthOptions>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = auth.Jwt.Emissor,
            ValidAudience = auth.Jwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Jwt.Chave)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy(AuthEndpoints.PoliticaLimiteEnvio, contexto =>
    {
        var config = contexto.RequestServices.GetRequiredService<IConfiguration>();
        var secao = config.GetSection("Auth:RecuperacaoSenha");
        var limite = int.TryParse(secao["LimiteRequisicoes"], out var l) ? l : 5;
        var janela = int.TryParse(secao["JanelaMinutos"], out var j) ? j : 15;

        return RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = TimeSpan.FromMinutes(janela),
                QueueLimit = 0,
            });
    });
    opcoes.OnRejected = async (contexto, cancellationToken) =>
    {
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
        {
            contexto.HttpContext.Response.Headers.RetryAfter =
                ((int)espera.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "Muitas solicitações. Tente novamente em instantes.",
            },
            cancellationToken);
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/openapi/v1.json", "InvoiSys API v1");
        opcoes.DocumentTitle = "InvoiSys API";
    });
}

// Migração no startup é opt-in por flag, nunca por ambiente (ADR-016).
if (app.Configuration.GetValue<bool>("Database:MigrarAoIniciar"))
{
    await using var escopo = app.Services.CreateAsyncScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<InvoiSysDbContext>();
    await contexto.Database.MigrateAsync();
}

await using (var escopoAuth = app.Services.CreateAsyncScope())
{
    await escopoAuth.ServiceProvider.GetRequiredService<AdminInicialSemeador>().SemearAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new StatusSaude("ok")))
    .WithName("Health")
    .WithSummary("Verificação de que a API está no ar.")
    .Produces<StatusSaude>();
app.MapAuthEndpoints();
app.MapReleaseEndpoints();
app.MapRevisaoEndpoints();
app.MapItemComunicadoEndpoints();

app.Run();
return 0;

static async Task<int> ExecutarHealthCheckAsync()
{
    var porta = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };

    try
    {
        var resposta = await http.GetAsync($"http://localhost:{porta}/health");
        return resposta.IsSuccessStatusCode ? 0 : 1;
    }
    catch (Exception exc) when (exc is HttpRequestException or TaskCanceledException)
    {
        return 1;
    }
}

public sealed record StatusSaude(string Status);

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
