using InvoiSys.Application.Usuarios;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Tests.Unit;

public sealed class AdministracaoUsuariosTests
{
    private sealed class RepositorioFake : IUsuarioRepository
    {
        public List<Usuario> Dados { get; } = [];
        public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Dados.FirstOrDefault(u => u.Id == id));
        public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Dados.FirstOrDefault(u => u.Email == Usuario.NormalizarEmail(email)));
        public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) => Task.FromResult(Dados.Count > 0);
        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Dados);
        public Task SalvarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            if (!Dados.Contains(usuario)) Dados.Add(usuario);
            return Task.CompletedTask;
        }
    }

    private sealed class HashFake : IHashSenha
    {
        public string Gerar(string senha) => $"hash:{senha}";
        public bool Verificar(string senha, string hash) => hash == Gerar(senha);
    }

    [Fact]
    public async Task Cadastrar_normaliza_email_e_persiste_apenas_hash()
    {
        var repositorio = new RepositorioFake();
        var casoDeUso = new AdministracaoUsuarios(repositorio, new HashFake());
        var usuario = await casoDeUso.CadastrarAsync("Ana", " ANA@EXAMPLE.COM ", Usuario.PapelAdmin, "senha-segura");
        Assert.Equal("ana@example.com", usuario.Email);
        Assert.Equal("hash:senha-segura", usuario.SenhaHash);
    }

    [Theory]
    [InlineData("", "SenhaInicialInvalidaException")]
    [InlineData("1234567", "SenhaInicialInvalidaException")]
    public async Task Cadastrar_recusa_senha_fraca(string senha, string _)
    {
        var casoDeUso = new AdministracaoUsuarios(new RepositorioFake(), new HashFake());
        await Assert.ThrowsAsync<SenhaInicialInvalidaException>(() =>
            casoDeUso.CadastrarAsync("Ana", "ana@example.com", Usuario.PapelAdmin, senha));
    }

    [Fact]
    public async Task Cadastrar_recusa_papel_desconhecido()
    {
        var casoDeUso = new AdministracaoUsuarios(new RepositorioFake(), new HashFake());
        await Assert.ThrowsAsync<PapelUsuarioInvalidoException>(() =>
            casoDeUso.CadastrarAsync("Ana", "ana@example.com", "operador", "senha-segura"));
    }

    [Fact]
    public async Task Desativar_recusa_auto_desativacao_e_desativa_outro_usuario()
    {
        var repositorio = new RepositorioFake();
        var admin = new Usuario("Admin", "admin@example.com", "hash", Usuario.PapelAdmin);
        var alvo = new Usuario("Alvo", "alvo@example.com", "hash", Usuario.PapelReleaseManager);
        repositorio.Dados.AddRange([admin, alvo]);
        var casoDeUso = new AdministracaoUsuarios(repositorio, new HashFake());
        await Assert.ThrowsAsync<AutoDesativacaoUsuarioException>(() => casoDeUso.DesativarAsync(admin.Id, admin.Id));
        await casoDeUso.DesativarAsync(alvo.Id, admin.Id);
        Assert.False(alvo.Ativo);
    }
}
