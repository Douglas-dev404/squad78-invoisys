using InvoiSys.Api.Endpoints;
using InvoiSys.Application;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Aplica migrations pendentes ao subir, quando explicitamente habilitado. Sem isso,
// `docker compose up` entrega uma API conectada a um banco vazio e a primeira query
// falha com "relation does not exist".
//
// É opt-in por flag, e não por ambiente, por dois motivos: em produção, migrar no
// startup de uma aplicação com várias instâncias faz todas correrem para aplicar a
// mesma migration, e um schema destrutivo entraria sem ninguém revisar — lá a
// migration é passo próprio do deploy. E os testes de integração sobem a aplicação
// sem banco nenhum: migrar no startup os faria falhar por timeout de conexão.
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
