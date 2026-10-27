using InvoiSys.Api.Endpoints;
using InvoiSys.Application;
using System.Text;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

app.MapGet("/health", () => Results.Ok(new StatusSaude("ok")))
    .WithName("Health")
    .WithSummary("Verificação de que a API está no ar.")
    .Produces<StatusSaude>();
app.MapReleaseEndpoints();
app.MapRevisaoEndpoints();
app.MapItemComunicadoEndpoints();
app.MapExportacaoEndpoints();

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
