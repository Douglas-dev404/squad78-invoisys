using InvoiSys.Api.Endpoints;
using InvoiSys.Application;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--healthcheck"))
{
    return await ExecutarHealthCheckAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

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
