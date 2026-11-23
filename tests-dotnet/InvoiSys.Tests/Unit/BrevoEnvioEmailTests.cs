using System.Net;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Email;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Unit;

public class BrevoEnvioEmailTests
{
    private static (BrevoEnvioEmail Adapter, FakeHttpMessageHandler Handler) Criar(
        Action<FakeHttpMessageHandler>? respostas = null)
    {
        var handler = new FakeHttpMessageHandler();
        respostas?.Invoke(handler);

        var adapter = new BrevoEnvioEmail(
            new HttpClient(handler),
            Options.Create(new EmailOptions
            {
                ApiKey = "chave-de-teste",
                Remetente = "nao-responda@invoisys.test",
                NomeRemetente = "InvoiSys",
            }),
            NullLogger<BrevoEnvioEmail>.Instance);

        return (adapter, handler);
    }

    [Fact]
    public async Task EnviarComunicado_monta_a_requisicao_da_Brevo_com_html_e_texto()
    {
        var (adapter, handler) = Criar(h => h.ResponderJson("""{ "messageId": "<1@brevo>" }""", HttpStatusCode.Created));

        await adapter.EnviarComunicadoAsync(
            "financeiro@acme.com", "Financeiro ACME", "Release 2026.10.0", "<h1>Oi</h1>", "Oi");

        handler.UrisChamadas.Should().ContainSingle()
            .Which!.ToString().Should().Be("https://api.brevo.com/v3/smtp/email");
        using var corpo = JsonDocument.Parse(handler.CorposEnviados.Single());
        var raiz = corpo.RootElement;
        raiz.GetProperty("sender").GetProperty("email").GetString().Should().Be("nao-responda@invoisys.test");
        raiz.GetProperty("to")[0].GetProperty("email").GetString().Should().Be("financeiro@acme.com");
        raiz.GetProperty("to")[0].GetProperty("name").GetString().Should().Be("Financeiro ACME");
        raiz.GetProperty("subject").GetString().Should().Be("Release 2026.10.0");
        raiz.GetProperty("htmlContent").GetString().Should().Be("<h1>Oi</h1>");
        raiz.GetProperty("textContent").GetString().Should().Be("Oi");
    }

    [Fact]
    public async Task EnviarRedefinicaoSenha_leva_o_link_e_escapa_o_nome_no_html()
    {
        var (adapter, handler) = Criar(h => h.ResponderJson("{}", HttpStatusCode.Created));

        await adapter.EnviarRedefinicaoSenhaAsync(
            "ana@acme.com", "<Ana>", "https://app/redefinir#token=abc", DateTimeOffset.UtcNow.AddMinutes(30));

        using var corpo = JsonDocument.Parse(handler.CorposEnviados.Single());
        var html = corpo.RootElement.GetProperty("htmlContent").GetString();
        html.Should().Contain("https://app/redefinir#token=abc").And.Contain("&lt;Ana&gt;").And.NotContain("<Ana>");
        corpo.RootElement.GetProperty("textContent").GetString().Should().Contain("https://app/redefinir#token=abc");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Erro_do_provedor_vira_EnvioEmailException_sem_o_corpo_da_resposta(HttpStatusCode status)
    {
        var (adapter, _) = Criar(h => h.ResponderJson("""{ "message": "segredo-do-provedor" }""", status));

        var enviar = () => adapter.EnviarComunicadoAsync("a@acme.com", "A", "Assunto", "<p>x</p>", "x");

        var erro = await enviar.Should().ThrowAsync<EnvioEmailException>();
        erro.Which.Message.Should().Be($"Provedor de e-mail retornou {(int)status}.");
        erro.Which.Message.Should().NotContain("segredo-do-provedor");
        erro.Which.Should().BeAssignableTo<IntegracaoExternaException>();
    }

    private static IEnvioEmail Resolver(params (string Chave, string? Valor)[] extras)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=x;Username=x;Password=x",
            ["Auth:Jwt:Chave"] = ConfiguracaoDeTestes.ChaveJwt,
            ["Auth:RecuperacaoSenha:UrlBase"] = ConfiguracaoDeTestes.UrlRedefinicao,
        };
        foreach (var (chave, valor) in extras)
        {
            valores[chave] = valor;
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        using var servicos = new ServiceCollection().AddLogging().AddInfrastructure(configuracao).BuildServiceProvider();
        using var escopo = servicos.CreateScope();
        return escopo.ServiceProvider.GetRequiredService<IEnvioEmail>();
    }

    [Fact]
    public void Sem_chave_o_composition_root_usa_o_EnvioEmailPendente()
    {
        Resolver().Should().BeOfType<EnvioEmailPendente>();
        Resolver(("Email:ApiKey", "chave")).Should().BeOfType<EnvioEmailPendente>();
    }

    [Fact]
    public void Com_chave_e_remetente_o_composition_root_usa_a_Brevo()
    {
        Resolver(("Email:ApiKey", "chave"), ("Email:Remetente", "nao-responda@invoisys.test"))
            .Should().BeOfType<BrevoEnvioEmail>();
    }
}
