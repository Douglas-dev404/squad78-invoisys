using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Integration;

public class AdminInicialTests
{
    private const string Senha = "SenhaSecretaDoAdmin#2026";

    private readonly FakeUsuarioRepository _usuarios = new();

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

    private Task Semear(AuthOptions opcoes, LoggerColetor? logger = null) =>
        new AdminInicialSemeador(
            _usuarios,
            new Argon2idHashSenha(),
            Options.Create(opcoes),
            logger ?? new LoggerColetor()).SemearAsync();

    [Fact]
    public async Task Cria_o_admin_uma_unica_vez_mesmo_com_varios_restarts()
    {
        var email = "Admin@InvoiSys.com ";

        await Semear(Opcoes(email));
        await Semear(Opcoes(email));
        await Semear(Opcoes(email));

        _usuarios.Usuarios.Should().ContainSingle();
        var admin = _usuarios.Usuarios[0];
        admin.Papel.Should().Be("admin");
        admin.Email.Should().Be("admin@invoisys.com");
        admin.Nome.Should().Be("Admin Inicial");
        new Argon2idHashSenha().Verificar(Senha, admin.SenhaHash).Should().BeTrue();
    }

    [Fact]
    public async Task Nao_cria_admin_se_ja_existe_algum_usuario()
    {
        _usuarios.Usuarios.Add(new Usuario("Ana", "ana@invoisys.com", "hash", "release_manager"));

        await Semear(Opcoes("admin@invoisys.com"));

        _usuarios.Usuarios.Should().ContainSingle();
    }

    [Theory]
    [InlineData("", Senha, "Nome")]
    [InlineData("admin@invoisys.com", "", "Nome")]
    [InlineData("admin@invoisys.com", Senha, " ")]
    public async Task Nao_cria_admin_se_faltar_alguma_variavel(string email, string senha, string nome)
    {
        await Semear(Opcoes(email, senha, nome));

        _usuarios.Usuarios.Should().BeEmpty();
    }

    [Fact]
    public async Task A_senha_nunca_aparece_no_log()
    {
        var logger = new LoggerColetor();

        await Semear(Opcoes("admin@invoisys.com"), logger);
        await Semear(Opcoes("admin@invoisys.com"), logger);

        logger.Mensagens.Should().NotBeEmpty();
        logger.Mensagens.Should().OnlyContain(m => !m.Contains(Senha));
    }
}
