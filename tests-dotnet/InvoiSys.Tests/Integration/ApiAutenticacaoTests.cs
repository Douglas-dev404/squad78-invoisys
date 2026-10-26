using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace InvoiSys.Tests.Integration;

public class ApiAutenticacaoTests
{
    private static readonly Guid ItemId = Guid.NewGuid();

    private static WebApplicationFactory<Program> CriarApp(params Usuario[] usuarios) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.RemoveAll<ITokenRedefinicaoSenhaRepository>();
                services.AddScoped<IReleaseRepository>(_ => new FakeReleaseRepository());
                services.AddSingleton<ITokenRedefinicaoSenhaRepository>(new FakeTokenRedefinicaoSenhaRepository());
                services.ComUsuarios(usuarios);
            }));

    public static TheoryData<string, string> RotasProtegidas => new()
    {
        { "GET", "/api/v1/releases" },
        { "GET", "/api/v1/releases/RELEASE-1" },
        { "GET", "/api/v1/releases/RELEASE-1/historias" },
        { "POST", "/api/v1/releases/RELEASE-1/processar" },
        { "POST", "/api/v1/releases/RELEASE-1/aprovar" },
        { "POST", "/api/v1/releases/RELEASE-1/reprovar" },
        { "POST", "/api/v1/releases/RELEASE-1/reabrir" },
        { "PATCH", $"/api/v1/releases/RELEASE-1/itens/{ItemId}" },
        { "POST", $"/api/v1/releases/RELEASE-1/itens/{ItemId}/excluir" },
        { "POST", $"/api/v1/releases/RELEASE-1/itens/{ItemId}/reincluir" },
    };

    private static Task<HttpResponseMessage> Chamar(HttpClient client, string metodo, string rota) =>
        client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), rota)
        {
            Content = metodo == "GET" ? null : JsonContent.Create(new { publico = "cliente", texto = "x", motivo = "y" }),
        });

    [Theory]
    [MemberData(nameof(RotasProtegidas))]
    public async Task Rota_protegida_sem_token_responde_401(string metodo, string rota)
    {
        using var app = CriarApp();
        using var client = app.CreateClient();

        var resposta = await Chamar(client, metodo, rota);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(RotasProtegidas))]
    public async Task Rota_protegida_com_token_valido_passa_da_autenticacao(string metodo, string rota)
    {
        using var app = CriarApp(AutenticacaoDeTeste.Revisor);
        using var client = app.CriarClienteAutenticado();

        var resposta = await Chamar(client, metodo, rota);

        resposta.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_malformado_expirado_ou_com_assinatura_errada_responde_401()
    {
        var usuario = AutenticacaoDeTeste.Revisor;
        using var app = CriarApp(usuario);
        var antigo = new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-9));

        var tokens = new[]
        {
            "lixo",
            AutenticacaoDeTeste.CriarToken(usuario, antigo),
            AutenticacaoDeTeste.CriarToken(usuario, chave: "outra-chave-bem-diferente-com-32-caracteres"),
        };

        foreach (var token in tokens)
        {
            using var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            (await client.GetAsync("/api/v1/releases")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Usuario_desativado_ou_inexistente_com_token_valido_responde_401()
    {
        var desativado = AutenticacaoDeTeste.UmRevisor("Desativado");
        desativado.Desativar();
        var fantasma = AutenticacaoDeTeste.UmRevisor("Fantasma");
        using var app = CriarApp(desativado);

        foreach (var usuario in new[] { desativado, fantasma })
        {
            using var client = app.CriarClienteAutenticado(usuario);

            (await client.GetAsync("/api/v1/releases")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Rotas_publicas_respondem_sem_token()
    {
        using var app = CriarApp(AutenticacaoDeTeste.Revisor);
        using var client = app.CreateClient();

        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "alguem@invoisys.com" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/auth/resend-forgot-password", new { email = "alguem@invoisys.com" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "x", novaSenha = "senha-nova-123" }))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public void Toda_rota_de_negocio_exige_autorizacao_e_so_as_de_auth_e_o_health_sao_publicas()
    {
        using var app = CriarApp();
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .ToList();

        endpoints.Should().NotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            var rota = endpoint.RoutePattern.RawText ?? string.Empty;
            var exigeAuth = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;
            var anonimo = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (rota.StartsWith("/api/v1/auth/", StringComparison.Ordinal) || rota == "/health")
            {
                (exigeAuth && !anonimo).Should().BeFalse($"{rota} deve ser pública");
            }
            else if (rota.StartsWith("/api/v1/", StringComparison.Ordinal))
            {
                (exigeAuth && !anonimo).Should().BeTrue($"{rota} deve exigir autenticação");
            }
        }
    }
}
