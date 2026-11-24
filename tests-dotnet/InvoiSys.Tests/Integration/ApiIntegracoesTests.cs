using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Integration;

public sealed class ApiIntegracoesTests
{
    private static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private sealed class RepositorioEmMemoria : IConfiguracaoIntegracaoRepository
    {
        public Dictionary<string, ConfiguracaoIntegracao> Linhas { get; } = [];

        public Task<IReadOnlyList<ConfiguracaoIntegracao>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConfiguracaoIntegracao>>([.. Linhas.Values]);

        public Task SalvarAsync(
            IReadOnlyCollection<ConfiguracaoIntegracao> gravar,
            IReadOnlyCollection<string> remover,
            CancellationToken cancellationToken = default)
        {
            foreach (var linha in gravar)
            {
                Linhas[linha.Chave] = linha;
            }

            foreach (var chave in remover)
            {
                Linhas.Remove(chave);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class LlmQueResponde : ILlmProvider
    {
        public Task<CategoriaAlteracao> CategorizarAsync(string textoFonte, CancellationToken cancellationToken = default) =>
            Task.FromResult(CategoriaAlteracao.Correcao);

        public Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
            IReadOnlyList<(string Chave, string Texto)> textos, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> ReescreverLinguagemNegocioAsync(
            IReadOnlyList<string> textosFonte, CategoriaAlteracao categoria, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
            IReadOnlyList<string> itensTexto, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class AutenticacaoTesteHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var papel = Request.Headers["X-Test-Role"].FirstOrDefault();
            var id = Request.Headers["X-Test-Sub"].FirstOrDefault();
            if (papel is null || id is null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identidade = new ClaimsIdentity(
                [new Claim("role", papel), new Claim("sub", id)], Scheme.Name, "name", "role");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name)));
        }
    }

    private static WebApplicationFactory<Program> CriarApp(RepositorioEmMemoria repositorio) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConfiguracaoIntegracaoRepository>();
                services.AddSingleton<IConfiguracaoIntegracaoRepository>(repositorio);
                services.RemoveAll<ILlmProvider>();
                services.AddScoped<ILlmProvider, LlmQueResponde>();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Teste";
                    options.DefaultChallengeScheme = "Teste";
                    options.DefaultScheme = "Teste";
                }).AddScheme<AuthenticationSchemeOptions, AutenticacaoTesteHandler>("Teste", _ => { });
            }));

    private static HttpRequestMessage Requisicao(HttpMethod metodo, string url, object? corpo = null, string? papel = "admin")
    {
        var request = new HttpRequestMessage(metodo, url);
        if (corpo is not null)
        {
            request.Content = JsonContent.Create(corpo);
        }

        if (papel is not null)
        {
            request.Headers.Add("X-Test-Role", papel);
            request.Headers.Add("X-Test-Sub", AdminId.ToString());
        }

        return request;
    }

    [Fact]
    public async Task Integracoes_exigem_login_e_papel_admin()
    {
        using var app = CriarApp(new RepositorioEmMemoria());
        using var client = app.CreateClient();

        using var semLogin = Requisicao(HttpMethod.Get, "/api/v1/integracoes", papel: null);
        (await client.SendAsync(semLogin)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        foreach (var (metodo, url) in new[]
        {
            (HttpMethod.Get, "/api/v1/integracoes"),
            (HttpMethod.Put, "/api/v1/integracoes/jira"),
            (HttpMethod.Put, "/api/v1/integracoes/ia"),
            (HttpMethod.Post, "/api/v1/integracoes/jira/testar"),
            (HttpMethod.Post, "/api/v1/integracoes/ia/testar"),
        })
        {
            using var gestor = Requisicao(metodo, url, new { }, papel: "release_manager");
            (await client.SendAsync(gestor)).StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{metodo} {url}");
        }
    }

    [Fact]
    public async Task Salvar_jira_guarda_cifrado_e_nenhuma_resposta_devolve_o_segredo()
    {
        var repositorio = new RepositorioEmMemoria();
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        using var salvar = Requisicao(HttpMethod.Put, "/api/v1/integracoes/jira", new
        {
            baseUrl = "https://acme.atlassian.net/",
            email = "dev@acme.com",
            projeto = "inv",
            apiToken = "token-super-secreto",
        });
        var resposta = await client.SendAsync(salvar);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.Should().NotContain("token-super-secreto");

        using var obter = Requisicao(HttpMethod.Get, "/api/v1/integracoes");
        var visao = await (await client.SendAsync(obter)).Content.ReadFromJsonAsync<JsonElement>();
        var jira = visao.GetProperty("jira");
        jira.GetProperty("baseUrl").GetString().Should().Be("https://acme.atlassian.net");
        jira.GetProperty("email").GetString().Should().Be("dev@acme.com");
        jira.GetProperty("projeto").GetString().Should().Be("INV");
        jira.GetProperty("tokenConfigurado").GetBoolean().Should().BeTrue();
        jira.GetProperty("tokenDoSistema").GetBoolean().Should().BeTrue();
        jira.GetProperty("configurado").GetBoolean().Should().BeTrue();
        visao.ToString().Should().NotContain("token-super-secreto");

        var guardado = repositorio.Linhas["jira.api_token"];
        guardado.Secreto.Should().BeTrue();
        guardado.Valor.Should().NotContain("token-super-secreto").And.StartWith("v1:");
    }

    [Fact]
    public async Task Salvar_com_segredo_em_branco_mantem_o_que_ja_estava()
    {
        var repositorio = new RepositorioEmMemoria();
        using var app = CriarApp(repositorio);
        using var client = app.CreateClient();

        using var primeira = Requisicao(HttpMethod.Put, "/api/v1/integracoes/ia",
            new { provedor = "OpenAi", modelo = "gpt-4o-mini", apiKey = "sk-ia-secreta" });
        (await client.SendAsync(primeira)).StatusCode.Should().Be(HttpStatusCode.OK);
        var cifrado = repositorio.Linhas["ia.openai.api_key"].Valor;

        using var segunda = Requisicao(HttpMethod.Put, "/api/v1/integracoes/ia",
            new { provedor = "OpenAi", modelo = "gpt-5", apiKey = "" });
        var visao = await (await client.SendAsync(segunda)).Content.ReadFromJsonAsync<JsonElement>();

        repositorio.Linhas["ia.openai.api_key"].Valor.Should().Be(cifrado);
        var ia = visao.GetProperty("ia");
        ia.GetProperty("provedor").GetString().Should().Be("OpenAi");
        ia.GetProperty("openAi").GetProperty("modelo").GetString().Should().Be("gpt-5");
        ia.GetProperty("openAi").GetProperty("chaveConfigurada").GetBoolean().Should().BeTrue();
        ia.GetProperty("configurado").GetBoolean().Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/integracoes/jira", "{\"baseUrl\":\"http://sem-https.com\"}")]
    [InlineData("/api/v1/integracoes/jira", "{\"email\":\"nao-e-email\"}")]
    [InlineData("/api/v1/integracoes/jira", "{\"projeto\":\"1 inválido!\"}")]
    [InlineData("/api/v1/integracoes/ia", "{\"provedor\":\"Gemini\"}")]
    public async Task Entrada_invalida_retorna_422(string url, string json)
    {
        using var app = CriarApp(new RepositorioEmMemoria());
        using var client = app.CreateClient();
        using var request = Requisicao(HttpMethod.Put, url);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Testar_ia_responde_ok_e_o_que_aconteceu_aparece_na_execucao_ao_vivo()
    {
        using var app = CriarApp(new RepositorioEmMemoria());
        using var client = app.CreateClient();

        using var testar = Requisicao(HttpMethod.Post, "/api/v1/integracoes/ia/testar");
        var teste = await (await client.SendAsync(testar)).Content.ReadFromJsonAsync<JsonElement>();
        teste.GetProperty("ok").GetBoolean().Should().BeTrue();

        using var aoVivo = Requisicao(HttpMethod.Get, "/api/v1/execucoes/ao-vivo?depois=0", papel: "release_manager");
        var resposta = await client.SendAsync(aoVivo);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var eventos = corpo.GetProperty("eventos").EnumerateArray().ToList();
        eventos.Should().NotBeEmpty();
        eventos[0].GetProperty("release").GetString().Should().Be("teste de conexão");
        corpo.GetProperty("ultimoId").GetInt64().Should().Be(eventos[^1].GetProperty("id").GetInt64());

        using var depois = Requisicao(HttpMethod.Get, $"/api/v1/execucoes/ao-vivo?depois={corpo.GetProperty("ultimoId").GetInt64()}");
        var vazio = await (await client.SendAsync(depois)).Content.ReadFromJsonAsync<JsonElement>();
        vazio.GetProperty("eventos").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Execucao_ao_vivo_exige_login()
    {
        using var app = CriarApp(new RepositorioEmMemoria());
        using var client = app.CreateClient();
        using var request = Requisicao(HttpMethod.Get, "/api/v1/execucoes/ao-vivo", papel: null);

        (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
