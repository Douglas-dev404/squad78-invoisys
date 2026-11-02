using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Integration;

public sealed class ApiUsuariosTests
{
    private static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private sealed class RepositorioFake : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios = [];
        public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Id == id));
        public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Email == Usuario.NormalizarEmail(email)));
        public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) => Task.FromResult(_usuarios.Count > 0);
        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.OrderBy(usuario => usuario.Nome).ToList());
        public Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            if (!_usuarios.Contains(usuario)) _usuarios.Add(usuario);
            return Task.CompletedTask;
        }
        public void Adicionar(Usuario usuario) => _usuarios.Add(usuario);
    }

    private sealed class HashFake : IHashSenha
    {
        public string Gerar(string senha) => $"hash:{senha}";
        public bool Verificar(string senha, string hash) => hash == Gerar(senha);
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
            if (papel is null || id is null) return Task.FromResult(AuthenticateResult.NoResult());
            var identidade = new ClaimsIdentity(
                [new Claim("role", papel), new Claim("sub", id)], Scheme.Name, "name", "role");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name)));
        }
    }

    private static (WebApplicationFactory<Program> App, RepositorioFake Repositorio) CriarApp()
    {
        var repositorio = new RepositorioFake();
        var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUsuarioRepository>();
                services.AddSingleton<IUsuarioRepository>(repositorio);
                services.RemoveAll<IHashSenha>();
                services.AddSingleton<IHashSenha, HashFake>();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Teste";
                    options.DefaultChallengeScheme = "Teste";
                    options.DefaultScheme = "Teste";
                }).AddScheme<AuthenticationSchemeOptions, AutenticacaoTesteHandler>("Teste", _ => { });
            }));
        return (app, repositorio);
    }

    private static void Autenticar(HttpRequestMessage request, string papel = "admin", Guid? id = null)
    {
        request.Headers.Add("X-Test-Role", papel);
        request.Headers.Add("X-Test-Sub", (id ?? AdminId).ToString());
    }

    private static HttpRequestMessage Cadastro(string nome = "Ana", string email = "ana@example.com",
        string papel = "release_manager", string senha = "senha-segura") =>
        new(HttpMethod.Post, "/api/v1/usuarios")
        {
            Content = JsonContent.Create(new { nome, email, papel, senhaInicial = senha }),
        };

    [Fact]
    public async Task Rotas_sem_autenticacao_retorna_401_e_usuario_sem_papel_admin_retorna_403()
    {
        var (app, _) = CriarApp();
        using (app)
        using (var client = app.CreateClient())
        {
            (await client.GetAsync("/api/v1/usuarios")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/usuarios");
            Autenticar(request, "release_manager");
            (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Cadastro_admin_retorna_201_sem_expor_hash_e_lista_sem_hash()
    {
        var (app, _) = CriarApp();
        using (app)
        using (var client = app.CreateClient())
        using (var request = Cadastro())
        {
            Autenticar(request);
            var resposta = await client.SendAsync(request);
            resposta.StatusCode.Should().Be(HttpStatusCode.Created);
            var corpo = await resposta.Content.ReadAsStringAsync();
            corpo.Should().NotContain("senhaHash").And.NotContain("hash:senha-segura");

            using var listar = new HttpRequestMessage(HttpMethod.Get, "/api/v1/usuarios");
            Autenticar(listar);
            var lista = await client.SendAsync(listar);
            lista.StatusCode.Should().Be(HttpStatusCode.OK);
            (await lista.Content.ReadAsStringAsync()).Should().NotContain("senhaHash").And.NotContain("hash:");
        }
    }

    [Fact]
    public async Task Cadastro_com_email_repetido_retorna_409()
    {
        var (app, _) = CriarApp();
        using (app)
        using (var client = app.CreateClient())
        {
            using var primeira = Cadastro();
            Autenticar(primeira);
            (await client.SendAsync(primeira)).StatusCode.Should().Be(HttpStatusCode.Created);
            using var repetida = Cadastro(email: " ANA@EXAMPLE.COM ");
            Autenticar(repetida);
            (await client.SendAsync(repetida)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("")]
    public async Task Cadastro_com_senha_fraca_retorna_422(string senha)
    {
        var (app, _) = CriarApp();
        using (app)
        using (var client = app.CreateClient())
        using (var request = Cadastro(senha: senha))
        {
            Autenticar(request);
            (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Desativacao_de_outro_usuario_retorna_204_e_propria_conta_retorna_422()
    {
        var (app, repositorio) = CriarApp();
        var alvo = new Usuario("Alvo", "alvo@example.com", "hash", Usuario.PapelReleaseManager);
        var admin = new Usuario("Admin", "admin@example.com", "hash", Usuario.PapelAdmin);
        repositorio.Adicionar(admin);
        repositorio.Adicionar(alvo);
        using (app)
        using (var client = app.CreateClient())
        {
            using var desativar = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/usuarios/{alvo.Id}/desativar");
            Autenticar(desativar);
            (await client.SendAsync(desativar)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            alvo.Ativo.Should().BeFalse();

            using var propriaConta = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/usuarios/{admin.Id}/desativar");
            Autenticar(propriaConta, id: admin.Id);
            (await client.SendAsync(propriaConta)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Desativacao_de_usuario_inexistente_retorna_404()
    {
        var (app, _) = CriarApp();
        using (app)
        using (var client = app.CreateClient())
        using (var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/usuarios/20000000-0000-0000-0000-000000000002/desativar"))
        {
            Autenticar(request);
            (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
