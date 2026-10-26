using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace InvoiSys.Tests.Integration;

public class ApiAuthRecuperacaoTests
{
    private sealed record Ambiente(
        WebApplicationFactory<Program> App,
        FakeUsuarioRepository Usuarios,
        FakeTokenRedefinicaoSenhaRepository Tokens,
        FakeEnvioEmail Email,
        FakeTimeProvider Relogio,
        Usuario Ana);

    private static Ambiente Criar(int limite = 100)
    {
        var usuarios = new FakeUsuarioRepository();
        var tokens = new FakeTokenRedefinicaoSenhaRepository();
        var email = new FakeEnvioEmail();
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
        var ana = new Usuario("Ana", "ana@invoisys.com", "hash:senha-antiga", "release_manager");
        usuarios.Usuarios.Add(ana);

        var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:RecuperacaoSenha:LimiteRequisicoes", limite.ToString());
            builder.UseSetting("Auth:RecuperacaoSenha:CooldownSegundos", "0");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<ITokenRedefinicaoSenhaRepository>();
                services.RemoveAll<IEnvioEmail>();
                services.RemoveAll<IHashSenha>();
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<IUsuarioRepository>(usuarios);
                services.AddSingleton<ITokenRedefinicaoSenhaRepository>(tokens);
                services.AddSingleton<IEnvioEmail>(email);
                services.AddSingleton<IHashSenha>(new FakeHashSenha());
                services.AddSingleton<TimeProvider>(relogio);
            });
        });

        return new Ambiente(app, usuarios, tokens, email, relogio, ana);
    }

    private static string Token(FakeEnvioEmail email) => email.Enviados[^1].Link.Split("#token=")[1];

    [Fact]
    public async Task Forgot_password_responde_igual_para_email_existente_e_inexistente()
    {
        var a = Criar();
        using var client = a.App.CreateClient();

        var existente = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ana@invoisys.com" });
        var inexistente = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ninguem@invoisys.com" });

        existente.StatusCode.Should().Be(HttpStatusCode.OK).And.Be(inexistente.StatusCode);
        var corpoExistente = await existente.Content.ReadAsStringAsync();
        corpoExistente.Should().Be(await inexistente.Content.ReadAsStringAsync());
        corpoExistente.Should().Contain("\"expiraEmMinutos\":30").And.Contain("\"mensagem\"");
        a.Email.Enviados.Should().ContainSingle();
    }

    [Fact]
    public async Task Fluxo_completo_troca_a_senha_e_o_token_nao_serve_de_novo()
    {
        var a = Criar();
        using var client = a.App.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ana@invoisys.com" });
        var token = Token(a.Email);

        var primeira = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, novaSenha = "senha-nova-123" });
        var segunda = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, novaSenha = "outra-senha-456" });

        primeira.StatusCode.Should().Be(HttpStatusCode.NoContent);
        segunda.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        a.Ana.SenhaHash.Should().Be("hash:senha-nova-123");
    }

    [Fact]
    public async Task Reset_com_token_expirado_ou_inventado_devolve_422()
    {
        var a = Criar();
        using var client = a.App.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ana@invoisys.com" });
        var token = Token(a.Email);
        a.Relogio.Advance(TimeSpan.FromMinutes(31));

        var expirado = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, novaSenha = "senha-nova-123" });
        var inventado = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "inventado", novaSenha = "senha-nova-123" });

        expirado.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        inventado.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await expirado.Content.ReadAsStringAsync()).Should().Be(await inventado.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Senha_curta_e_email_em_branco_devolvem_422()
    {
        var a = Criar();
        using var client = a.App.CreateClient();

        var senha = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "x", novaSenha = "curta" });
        var email = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "" });

        senha.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        email.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("/api/v1/auth/forgot-password")]
    [InlineData("/api/v1/auth/resend-forgot-password")]
    public async Task Rate_limit_devolve_429_com_Retry_After_depois_do_limite(string rota)
    {
        var a = Criar(limite: 2);
        using var client = a.App.CreateClient();

        var respostas = new List<HttpResponseMessage>();
        for (var i = 0; i < 3; i++)
        {
            respostas.Add(await client.PostAsJsonAsync(rota, new { email = "ana@invoisys.com" }));
        }

        respostas.Select(r => r.StatusCode).Should().Equal(
            HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        respostas[2].Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task Os_dois_endpoints_de_envio_dividem_o_mesmo_limite()
    {
        var a = Criar(limite: 2);
        using var client = a.App.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ana@invoisys.com" });
        await client.PostAsJsonAsync("/api/v1/auth/resend-forgot-password", new { email = "ana@invoisys.com" });
        var terceira = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "ana@invoisys.com" });

        terceira.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
