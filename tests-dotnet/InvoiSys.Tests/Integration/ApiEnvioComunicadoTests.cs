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

public class ApiEnvioComunicadoTests
{
    private const string Chave = "RELEASE-2026-10";

    private readonly FakeReleaseRepository _releases = new();
    private readonly FakeComunicadoExportadoRepository _exportados = new();
    private readonly FakeDestinatarioRepository _destinatarios = new();
    private readonly FakeEnvioComunicadoRepository _envios = new();
    private readonly FakeEnvioEmail _email = new();
    private readonly Destinatario _financeiro = new("Financeiro ACME", "financeiro@acme.com", PublicoAlvo.Cliente);

    public ApiEnvioComunicadoTests() => _destinatarios.Com(_financeiro);

    private WebApplicationFactory<Program> CriarApp() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBloqueioProcessamento>();
                services.AddSingleton<IBloqueioProcessamento>(new FakeBloqueioProcessamento());
                services.RemoveAll<IReleaseRepository>();
                services.RemoveAll<IComunicadoExportadoRepository>();
                services.RemoveAll<IDestinatarioRepository>();
                services.RemoveAll<IEnvioComunicadoRepository>();
                services.RemoveAll<IEnvioEmail>();
                services.AddSingleton<IReleaseRepository>(_releases);
                services.AddSingleton<IComunicadoExportadoRepository>(_exportados);
                services.AddSingleton<IDestinatarioRepository>(_destinatarios);
                services.AddSingleton<IEnvioComunicadoRepository>(_envios);
                services.AddSingleton<IEnvioEmail>(_email);
                services.ComUsuarios(AutenticacaoDeTeste.Revisor);
            }));

    private async Task ReleaseSalva(bool aprovada)
    {
        var release = new Release(Chave, []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Correcao, "ICMS-ST corrigido.", ["INV-4"])],
            "Correções de outubro",
            "Resumo");
        if (aprovada)
        {
            release.Aprovar(AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);
        }

        await _releases.SalvarAsync(release);
    }

    private async Task<HttpResponseMessage> Enviar(object corpo, string chave = Chave, bool autenticado = true)
    {
        using var app = CriarApp();
        using var client = autenticado ? app.CriarClienteAutenticado() : app.CreateClient();
        return await client.PostAsJsonAsync($"/api/v1/releases/{chave}/enviar", corpo);
    }

    [Fact]
    public async Task Enviar_responde_201_com_o_resultado_por_destinatario()
    {
        await ReleaseSalva(aprovada: true);

        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("publico").GetString().Should().Be("cliente");
        corpo.GetProperty("enviadoPor").GetString().Should().Be(AutenticacaoDeTeste.Revisor.Nome);
        var envio = corpo.GetProperty("envios")[0];
        envio.GetProperty("destinatarioId").GetGuid().Should().Be(_financeiro.Id);
        envio.GetProperty("email").GetString().Should().Be("financeiro@acme.com");
        envio.GetProperty("status").GetString().Should().Be("enviado");
        envio.GetProperty("erro").ValueKind.Should().Be(JsonValueKind.Null);
        resposta.Headers.Location!.ToString()
            .Should().Be($"/api/v1/exportacoes/{corpo.GetProperty("exportacaoId").GetGuid()}");
        _email.Comunicados.Should().ContainSingle().Which.Assunto.Should().Be("Correções de outubro");
    }

    [Fact]
    public async Task Detalhe_da_exportacao_traz_a_lista_de_envios()
    {
        await ReleaseSalva(aprovada: true);
        var envio = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } });
        var id = (await envio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exportacaoId").GetGuid();

        using var app = CriarApp();
        using var client = app.CriarClienteAutenticado();
        var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/v1/exportacoes/{id}");

        detalhe.GetProperty("formato").GetString().Should().Be("html");
        detalhe.GetProperty("envios").EnumerateArray().Select(e => e.GetProperty("email").GetString())
            .Should().Equal("financeiro@acme.com");
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        await ReleaseSalva(aprovada: true);

        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } }, autenticado: false);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Release_inexistente_responde_404()
    {
        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } }, "NAO-EXISTE");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Versao_nao_aprovada_responde_409_sem_enviar()
    {
        await ReleaseSalva(aprovada: false);

        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _email.Comunicados.Should().BeEmpty();
    }

    [Fact]
    public async Task Destinatario_de_outro_publico_responde_422_citando_o_email()
    {
        await ReleaseSalva(aprovada: true);
        var interno = new Destinatario("Dev", "dev@invoisys.com", PublicoAlvo.Interno);
        _destinatarios.Com(interno);

        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { interno.Id } });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .Should().Contain("dev@invoisys.com");
    }

    [Theory]
    [MemberData(nameof(CorposInvalidos))]
    public async Task Corpo_invalido_responde_422(object corpo)
    {
        await ReleaseSalva(aprovada: true);

        var resposta = await Enviar(corpo);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    public static TheoryData<object> CorposInvalidos()
    {
        var id = Guid.NewGuid();
        return new()
        {
            new { publico = "cliente", destinatarioIds = Array.Empty<Guid>() },
            new { publico = "cliente" },
            new { publico = "diretoria", destinatarioIds = new[] { id } },
            new { publico = "cliente", destinatarioIds = new[] { id, id } },
        };
    }

    [Fact]
    public async Task Provedor_fora_do_ar_para_todos_responde_502()
    {
        await ReleaseSalva(aprovada: true);
        _email.EmailsQueFalham.Add("financeiro@acme.com");

        var resposta = await Enviar(new { publico = "cliente", destinatarioIds = new[] { _financeiro.Id } });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        _envios.Envios.Should().ContainSingle().Which.Status.Should().Be(StatusEnvio.Falhou);
    }
}
