using InvoiSys.Api.Endpoints;
using InvoiSys.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new StatusSaude("ok")))
    .WithName("Health")
    .WithSummary("Verificação de que a API está no ar.")
    .Produces<StatusSaude>();
app.MapReleaseEndpoints();

app.Run();

/// <summary>Resposta do endpoint de health check.</summary>
public sealed record StatusSaude(string Status);

/// <summary>
/// Exposto para que os testes de integração possam instanciar a aplicação com
/// WebApplicationFactory. Não tem outro propósito em produção.
/// </summary>
public partial class Program;
