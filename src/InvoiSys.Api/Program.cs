using InvoiSys.Api.Endpoints;
using InvoiSys.Infrastructure;

// Modo health check: usado pelo HEALTHCHECK do Docker. A imagem base do runtime
// ASP.NET não traz curl nem wget, e instalar um pacote na imagem final só para isso
// aumentaria a superfície de ataque do container — a própria aplicação consulta o
// endpoint e traduz o resultado em código de saída.
if (args.Contains("--healthcheck"))
{
    return await ExecutarHealthCheckAsync();
}

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
return 0;

static async Task<int> ExecutarHealthCheckAsync()
{
    // Porta lida do ambiente para o check não mentir caso ASPNETCORE_URLS mude.
    var porta = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };

    try
    {
        var resposta = await http.GetAsync($"http://localhost:{porta}/health");
        return resposta.IsSuccessStatusCode ? 0 : 1;
    }
    catch (Exception exc) when (exc is HttpRequestException or TaskCanceledException)
    {
        // API fora do ar ou sem responder a tempo — é exatamente o que o health
        // check precisa reportar como falha, não como crash.
        return 1;
    }
}

/// <summary>Resposta do endpoint de health check.</summary>
public sealed record StatusSaude(string Status);

/// <summary>
/// Exposto para que os testes de integração possam instanciar a aplicação com
/// WebApplicationFactory. Não tem outro propósito em produção.
/// </summary>
public partial class Program;
