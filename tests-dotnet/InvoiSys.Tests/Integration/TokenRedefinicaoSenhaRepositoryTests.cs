using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class TokenRedefinicaoSenhaRepositoryTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    private async Task<Usuario> NovoUsuario()
    {
        var usuario = new Usuario("Ana", $"ana-{Guid.NewGuid():N}@invoisys.com", "hash", "release_manager");
        await using var contexto = fixture.CriarContexto();
        await new UsuarioRepository(contexto).SalvarAsync(usuario);
        return usuario;
    }

    private async Task<TokenRedefinicaoSenha> Emitir(Guid usuarioId, DateTimeOffset? agora = null)
    {
        var token = TokenRedefinicaoSenha.Emitir(usuarioId, Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), agora ?? Agora);
        await using var contexto = fixture.CriarContexto();
        await new TokenRedefinicaoSenhaRepository(contexto).AdicionarAsync(token);
        return token;
    }

    private async Task<bool> Consumir(Guid id, DateTimeOffset agora)
    {
        await using var contexto = fixture.CriarContexto();
        return await new TokenRedefinicaoSenhaRepository(contexto).ConsumirAsync(id, agora);
    }

    [Fact]
    public async Task Persiste_e_busca_por_hash_com_todos_os_campos()
    {
        var usuario = await NovoUsuario();
        var token = await Emitir(usuario.Id);

        await using var contexto = fixture.CriarContexto();
        var lido = await new TokenRedefinicaoSenhaRepository(contexto).BuscarPorHashAsync(token.TokenHash);

        lido.Should().NotBeNull();
        lido!.UsuarioId.Should().Be(usuario.Id);
        lido.ExpiraEm.Should().BeCloseTo(Agora.AddMinutes(30), TimeSpan.FromMilliseconds(1));
        lido.UsadoEm.Should().BeNull();
    }

    [Fact]
    public async Task Consumir_so_vence_uma_vez_mesmo_em_concorrencia()
    {
        var token = await Emitir((await NovoUsuario()).Id);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Consumir(token.Id, Agora)));

        resultados.Count(r => r).Should().Be(1);
    }

    [Fact]
    public async Task Consumir_token_expirado_nao_consome()
    {
        var token = await Emitir((await NovoUsuario()).Id);

        (await Consumir(token.Id, Agora.AddMinutes(31))).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidarPendentes_so_atinge_tokens_do_usuario_e_ainda_nao_usados()
    {
        var ana = await NovoUsuario();
        var bia = await NovoUsuario();
        var daAna = await Emitir(ana.Id);
        var daBia = await Emitir(bia.Id);

        await using (var contexto = fixture.CriarContexto())
        {
            await new TokenRedefinicaoSenhaRepository(contexto).InvalidarPendentesDoUsuarioAsync(ana.Id, Agora);
        }

        (await Consumir(daAna.Id, Agora)).Should().BeFalse();
        (await Consumir(daBia.Id, Agora)).Should().BeTrue();
    }

    [Fact]
    public async Task BuscarUltimoDoUsuario_devolve_o_mais_recente()
    {
        var usuario = await NovoUsuario();
        await Emitir(usuario.Id, Agora.AddMinutes(-10));
        var recente = await Emitir(usuario.Id, Agora);

        await using var contexto = fixture.CriarContexto();
        var ultimo = await new TokenRedefinicaoSenhaRepository(contexto).BuscarUltimoDoUsuarioAsync(usuario.Id);

        ultimo!.Id.Should().Be(recente.Id);
    }
}
