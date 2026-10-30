using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiAuthTests
{
    private const string Email = "ana@invoisys.com";
    private const string Senha = "Senha-Correta-123";

    private static async Task<FakeUsuarioRepository> UsuariosComAna(bool ativo = true)
    {
        var usuarios = new FakeUsuarioRepository();
        var ana = new Usuario("Ana Revisora", Email, new Argon2idHashSenha().Gerar(Senha), "release_manager");
        if (!ativo)
        {
            ana.Desativar();
        }

        await usuarios.SalvarAsync(ana);
        return usuarios;
    }

    private static WebApplicationFactory<Program> CriarApp(
        FakeUsuarioRepository usuarios,
        params (string Chave, string Valor)[] configuracoes) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (chave, valor) in configuracoes)
            {
                builder.UseSetting(chave, valor);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUsuarioRepository>();
                services.AddScoped<IUsuarioRepository>(_ => usuarios);
            });
        });

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string senha) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });

    private static async Task<JsonDocument> Json(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

    [Fact]
    public async Task Login_ok_devolve_usuario_token_e_expiracao_sem_expor_o_hash()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        var resposta = await Login(client, Email, Senha);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var corpo = await Json(resposta);
        corpo.RootElement.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        corpo.RootElement.GetProperty("expiraEm").GetDateTimeOffset().Should().BeAfter(DateTimeOffset.UtcNow);

        var usuario = corpo.RootElement.GetProperty("usuario");
        usuario.GetProperty("email").GetString().Should().Be(Email);
        usuario.GetProperty("nome").GetString().Should().Be("Ana Revisora");
        usuario.GetProperty("papel").GetString().Should().Be("release_manager");
        usuario.TryGetProperty("criadoEm", out _).Should().BeTrue();
        usuario.TryGetProperty("avatarUrl", out _).Should().BeTrue();
        usuario.TryGetProperty("senhaHash", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_aceita_email_com_caixa_e_espacos_diferentes()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        (await Login(client, "  ANA@invoisys.com ", Senha)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Senha_errada_e_email_inexistente_devolvem_o_mesmo_401()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        var senhaErrada = await Login(client, Email, "outra-senha");
        var emailInexistente = await Login(client, "ninguem@invoisys.com", Senha);

        senhaErrada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        emailInexistente.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var a = await Json(senhaErrada);
        using var b = await Json(emailInexistente);
        a.RootElement.GetProperty("detail").GetString()
            .Should().Be(b.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Usuario_desativado_nao_loga_e_recebe_o_mesmo_401()
    {
        using var app = CriarApp(await UsuariosComAna(ativo: false));
        using var client = app.CreateClient();

        var resposta = await Login(client, Email, Senha);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var corpo = await Json(resposta);
        corpo.RootElement.GetProperty("detail").GetString().Should().Be("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task Login_sem_email_ou_senha_devolve_422_com_erros_por_campo()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        var resposta = await Login(client, "", "");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var corpo = await Json(resposta);
        corpo.RootElement.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_acima_do_limite_por_minuto_devolve_429()
    {
        using var app = CriarApp(await UsuariosComAna(), ("RateLimit:Login:Tentativas", "2"));
        using var client = app.CreateClient();

        (await Login(client, Email, "errada")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Login(client, Email, "errada")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var limitada = await Login(client, Email, "errada");
        limitada.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limitada.Content.Headers.ContentType!.MediaType.Should().Contain("problem+json");
    }

    [Fact]
    public async Task Me_com_token_devolve_o_usuario()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        using var login = await Json(await Login(client, Email, Senha));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.RootElement.GetProperty("token").GetString());

        var resposta = await client.GetAsync("/api/v1/auth/me");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var corpo = await Json(resposta);
        corpo.RootElement.GetProperty("email").GetString().Should().Be(Email);
    }

    [Fact]
    public async Task Me_sem_token_devolve_401_em_problem_details()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/auth/me");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resposta.Content.Headers.ContentType!.MediaType.Should().Contain("problem+json");
    }

    [Fact]
    public async Task Me_com_token_invalido_devolve_401()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.aqui");

        (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_devolve_204()
    {
        using var app = CriarApp(await UsuariosComAna());
        using var client = app.CreateClient();

        (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cors_libera_somente_a_origem_configurada()
    {
        using var app = CriarApp(await UsuariosComAna(), ("Cors:Origens:0", "http://localhost:5173"));
        using var client = app.CreateClient();

        async Task<HttpResponseMessage> ComOrigem(string origem)
        {
            var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { email = Email, senha = Senha }),
            };
            requisicao.Headers.Add("Origin", origem);
            return await client.SendAsync(requisicao);
        }

        (await ComOrigem("http://localhost:5173")).Headers
            .GetValues("Access-Control-Allow-Origin").Should().Equal("http://localhost:5173");
        (await ComOrigem("http://site-malicioso.com")).Headers
            .Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task Cors_responde_o_preflight_do_navegador_para_a_origem_configurada()
    {
        using var app = CriarApp(await UsuariosComAna(), ("Cors:Origens:0", "http://localhost:5173"));
        using var client = app.CreateClient();

        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var resposta = await client.SendAsync(preflight);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        resposta.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("http://localhost:5173");
    }
}