using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiDestinatariosTests
{
    private static WebApplicationFactory<Program> CriarApp(FakeDestinatarioRepository destinatarios) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBloqueioProcessamento>();
                services.AddSingleton<IBloqueioProcessamento>(new FakeBloqueioProcessamento());
                services.RemoveAll<IDestinatarioRepository>();
                services.AddSingleton<IDestinatarioRepository>(destinatarios);
                services.ComUsuarios(AutenticacaoDeTeste.Revisor);
            }));

    [Fact]
    public async Task Rotas_exigem_token()
    {
        using var app = CriarApp(new FakeDestinatarioRepository());
        using var client = app.CreateClient();

        (await client.GetAsync("/api/v1/destinatarios")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/v1/destinatarios", new { nome = "x", email = "x@x.com", publico = "cliente" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Release_manager_cadastra_e_recebe_201_com_o_contrato()
    {
        var repositorio = new FakeDestinatarioRepository();
        using var app = CriarApp(repositorio);
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.PostAsJsonAsync(
            "/api/v1/destinatarios",
            new { nome = "Financeiro ACME", email = " Financeiro@ACME.com ", publico = "cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("nome").GetString().Should().Be("Financeiro ACME");
        corpo.GetProperty("email").GetString().Should().Be("financeiro@acme.com");
        corpo.GetProperty("publico").GetString().Should().Be("cliente");
        corpo.GetProperty("ativo").GetBoolean().Should().BeTrue();
        resposta.Headers.Location!.ToString().Should().Be($"/api/v1/destinatarios/{corpo.GetProperty("id").GetGuid()}");
        repositorio.Destinatarios.Should().ContainSingle();
    }

    [Fact]
    public async Task Listar_filtra_por_publico_e_so_traz_inativos_quando_pedido()
    {
        var inativo = new Destinatario("Antigo", "antigo@acme.com", PublicoAlvo.Cliente);
        inativo.Desativar();
        var repositorio = new FakeDestinatarioRepository().Com(
            new Destinatario("Ativo", "ativo@acme.com", PublicoAlvo.Cliente),
            inativo,
            new Destinatario("Suporte", "sup@acme.com", PublicoAlvo.Suporte));
        using var app = CriarApp(repositorio);
        using var client = app.CriarClienteAutenticado();

        var ativos = await client.GetFromJsonAsync<JsonElement>("/api/v1/destinatarios?publico=cliente");
        var todos = await client.GetFromJsonAsync<JsonElement>("/api/v1/destinatarios?publico=cliente&incluirInativos=true");

        ativos.EnumerateArray().Select(d => d.GetProperty("email").GetString()).Should().Equal("ativo@acme.com");
        todos.EnumerateArray().Select(d => d.GetProperty("email").GetString())
            .Should().Equal("antigo@acme.com", "ativo@acme.com");
    }

    [Fact]
    public async Task Email_repetido_no_mesmo_publico_responde_409()
    {
        var repositorio = new FakeDestinatarioRepository()
            .Com(new Destinatario("ACME", "contato@acme.com", PublicoAlvo.Cliente));
        using var app = CriarApp(repositorio);
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.PostAsJsonAsync(
            "/api/v1/destinatarios",
            new { nome = "Outro", email = "CONTATO@acme.com", publico = "cliente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "a@acme.com", "cliente", "nome")]
    [InlineData("A", "nao-e-email", "cliente", "email")]
    [InlineData("A", "a@acme.com", "diretoria", "publico")]
    [InlineData("A", "a@acme.com", null, "publico")]
    public async Task Entrada_invalida_responde_422_com_o_campo(string nome, string email, string? publico, string campo)
    {
        using var app = CriarApp(new FakeDestinatarioRepository());
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.PostAsJsonAsync("/api/v1/destinatarios", new { nome, email, publico });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("errors").TryGetProperty(campo, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Filtro_de_publico_invalido_responde_422()
    {
        using var app = CriarApp(new FakeDestinatarioRepository());
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.GetAsync("/api/v1/destinatarios?publico=diretoria");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Alterar_responde_200_e_desativar_reativar_respondem_204()
    {
        var destinatario = new Destinatario("ACME", "contato@acme.com", PublicoAlvo.Cliente);
        using var app = CriarApp(new FakeDestinatarioRepository().Com(destinatario));
        using var client = app.CriarClienteAutenticado();

        var alterar = await client.PutAsJsonAsync(
            $"/api/v1/destinatarios/{destinatario.Id}",
            new { nome = "ACME Ltda", email = "contato@acme.com", publico = "comercial" });
        alterar.StatusCode.Should().Be(HttpStatusCode.OK);
        destinatario.Publico.Should().Be(PublicoAlvo.Comercial);

        (await client.PostAsync($"/api/v1/destinatarios/{destinatario.Id}/desativar", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        destinatario.Ativo.Should().BeFalse();

        (await client.PostAsync($"/api/v1/destinatarios/{destinatario.Id}/reativar", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        destinatario.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Id_inexistente_responde_404()
    {
        using var app = CriarApp(new FakeDestinatarioRepository());
        using var client = app.CriarClienteAutenticado();
        var id = Guid.NewGuid();

        (await client.PutAsJsonAsync($"/api/v1/destinatarios/{id}", new { nome = "x", email = "x@x.com", publico = "cliente" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync($"/api/v1/destinatarios/{id}/desativar", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync($"/api/v1/destinatarios/{id}/reativar", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
