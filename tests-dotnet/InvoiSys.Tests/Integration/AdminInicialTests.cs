using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class AdminInicialTests(PostgresContainerFixture fixture)
{
    private const string Senha = "SenhaSecretaDoAdmin#2026";

    private sealed class LoggerColetor : ILogger<AdminInicialSemeador>
    {
        public List<string> Mensagens { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Mensagens.Add(formatter(state, exception) + exception);
    }

    private static AuthOptions Opcoes(string email, string senha = Senha, string nome = "Admin Inicial") => new()
    {
        AdminInicial = new AdminInicialOptions { Email = email, Senha = senha, Nome = nome },
    };

    private async Task Semear(AuthOptions opcoes, LoggerColetor? logger = null)
    {
        await using var contexto = fixture.CriarContexto();
        var semeador = new AdminInicialSemeador(
            new UsuarioRepository(contexto),
            new Argon2idHashSenha(),
            Options.Create(opcoes),
            logger ?? new LoggerColetor());
        await semeador.SemearAsync();
    }

    private async Task LimparUsuarios()
    {
        await using var contexto = fixture.CriarContexto();
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM usuarios");
    }

    [Fact]
    public async Task Cria_o_admin_uma_unica_vez_mesmo_com_varios_restarts()
    {
        await LimparUsuarios();
        var email = "Admin@InvoiSys.com ";

        await Semear(Opcoes(email));
        await Semear(Opcoes(email));
        await Semear(Opcoes(email));

        await using var contexto = fixture.CriarContexto();
        var usuarios = await contexto.Usuarios.AsNoTracking().ToListAsync();
        usuarios.Should().ContainSingle();
        usuarios[0].Papel.Should().Be("admin");
        usuarios[0].Email.Should().Be("admin@invoisys.com");
        usuarios[0].Nome.Should().Be("Admin Inicial");
        new Argon2idHashSenha().Verificar(Senha, usuarios[0].SenhaHash).Should().BeTrue();
    }

    [Fact]
    public async Task Nao_cria_admin_se_ja_existe_algum_usuario()
    {
        await LimparUsuarios();
        await using (var contexto = fixture.CriarContexto())
        {
            await new UsuarioRepository(contexto).SalvarAsync(
                new Usuario("Ana", "ana@invoisys.com", "hash", "release_manager"));
        }

        await Semear(Opcoes("admin@invoisys.com"));

        await using var verificacao = fixture.CriarContexto();
        (await verificacao.Usuarios.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("", Senha, "Nome")]
    [InlineData("admin@invoisys.com", "", "Nome")]
    [InlineData("admin@invoisys.com", Senha, " ")]
    public async Task Nao_cria_admin_se_faltar_alguma_variavel(string email, string senha, string nome)
    {
        await LimparUsuarios();

        await Semear(Opcoes(email, senha, nome));

        await using var contexto = fixture.CriarContexto();
        (await contexto.Usuarios.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_senha_nunca_aparece_no_log()
    {
        await LimparUsuarios();
        var logger = new LoggerColetor();

        await Semear(Opcoes("admin@invoisys.com"), logger);
        await Semear(Opcoes("admin@invoisys.com"), logger);

        logger.Mensagens.Should().NotBeEmpty();
        logger.Mensagens.Should().OnlyContain(m => !m.Contains(Senha));
    }
}
