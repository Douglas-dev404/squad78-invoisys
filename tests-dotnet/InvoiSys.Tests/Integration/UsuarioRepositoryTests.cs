using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

/// <summary>Testes de integração de <see cref="UsuarioRepository"/> contra Postgres real.</summary>
[Collection("Postgres")]
public class UsuarioRepositoryTests(PostgresContainerFixture fixture)
{
    // E-mail tem índice único global: sufixo aleatório isola os testes entre si.
    private static Usuario UmUsuario(string? email = null) =>
        new("Ana Revisora", email ?? $"ana-{Guid.NewGuid():N}@invoisys.com", "hash-bcrypt", "release_manager");

    private async Task Salvar(Usuario usuario)
    {
        await using var contexto = fixture.CriarContexto();
        await new UsuarioRepository(contexto).SalvarAsync(usuario);
    }

    [Fact]
    public async Task SalvarAsync_persiste_e_as_buscas_por_id_e_email_trazem_todos_os_campos()
    {
        var usuario = UmUsuario();
        await Salvar(usuario);

        await using var contexto = fixture.CriarContexto();
        var repositorio = new UsuarioRepository(contexto);
        var porId = await repositorio.BuscarPorIdAsync(usuario.Id);
        var porEmail = await repositorio.BuscarPorEmailAsync(usuario.Email);

        porId.Should().NotBeNull();
        porId!.Nome.Should().Be("Ana Revisora");
        porId.SenhaHash.Should().Be("hash-bcrypt");
        porId.Papel.Should().Be("release_manager");
        porId.Ativo.Should().BeTrue();
        porEmail!.Id.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task Buscas_devolvem_null_quando_nao_existe()
    {
        await using var contexto = fixture.CriarContexto();
        var repositorio = new UsuarioRepository(contexto);

        (await repositorio.BuscarPorIdAsync(Guid.NewGuid())).Should().BeNull();
        (await repositorio.BuscarPorEmailAsync("ninguem@invoisys.com")).Should().BeNull();
    }

    [Fact]
    public async Task SalvarAsync_sobre_usuario_carregado_atualiza_sem_duplicar()
    {
        var usuario = UmUsuario();
        await Salvar(usuario);

        await using (var atualizacao = fixture.CriarContexto())
        {
            var repositorio = new UsuarioRepository(atualizacao);
            var carregado = await repositorio.BuscarPorIdAsync(usuario.Id);
            carregado!.Desativar();
            carregado.AtualizarAvatar("https://cdn.invoisys.com/ana.png");
            await repositorio.SalvarAsync(carregado);
        }

        await using var leitura = fixture.CriarContexto();
        var recarregado = await new UsuarioRepository(leitura).BuscarPorEmailAsync(usuario.Email);

        recarregado!.Ativo.Should().BeFalse();
        recarregado.AvatarUrl.Should().Be("https://cdn.invoisys.com/ana.png");
        (await leitura.Usuarios.CountAsync(u => u.Email == usuario.Email)).Should().Be(1);
    }

    [Fact]
    public async Task SalvarAsync_com_email_ja_cadastrado_lanca_DbUpdateException()
    {
        var email = $"dup-{Guid.NewGuid():N}@invoisys.com";
        await Salvar(UmUsuario(email));

        var acao = async () => await Salvar(UmUsuario(email));

        await acao.Should().ThrowAsync<DbUpdateException>();
    }
}
