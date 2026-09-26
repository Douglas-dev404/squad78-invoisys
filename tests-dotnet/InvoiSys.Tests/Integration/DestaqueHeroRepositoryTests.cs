using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração de <see cref="DestaqueHeroRepository"/> contra Postgres real
/// (Testcontainers). O container é compartilhado com as outras classes de banco, então
/// cada teste olha só para os destaques que ele mesmo semeou.
/// </summary>
[Collection("Postgres")]
public class DestaqueHeroRepositoryTests(PostgresContainerFixture fixture)
{
    private static DestaqueHero UmDestaque(int ordem, string titulo = "Título") =>
        new(titulo, "Descrição", "auto_awesome", ordem);

    private async Task Semear(params DestaqueHero[] destaques)
    {
        await using var contexto = fixture.CriarContexto();
        contexto.DestaquesHero.AddRange(destaques);
        await contexto.SaveChangesAsync();
    }

    private async Task<List<DestaqueHero>> ListarAtivosDentre(params DestaqueHero[] semeados)
    {
        var ids = semeados.Select(d => d.Id).ToHashSet();

        await using var contexto = fixture.CriarContexto();
        var ativos = await new DestaqueHeroRepository(contexto).ListarAtivosAsync();

        return [.. ativos.Where(d => ids.Contains(d.Id))];
    }

    [Fact]
    public async Task ListarAtivosAsync_devolve_o_destaque_com_todos_os_campos()
    {
        var destaque = new DestaqueHero("Release Notes com IA", "Gera comunicados", "auto_awesome", 1);
        await Semear(destaque);

        var encontrado = (await ListarAtivosDentre(destaque)).Single();

        encontrado.Titulo.Should().Be("Release Notes com IA");
        encontrado.Descricao.Should().Be("Gera comunicados");
        encontrado.Icone.Should().Be("auto_awesome");
        encontrado.Ordem.Should().Be(1);
        encontrado.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task ListarAtivosAsync_ordena_pela_ordem_de_exibicao()
    {
        var terceiro = UmDestaque(3, "Terceiro");
        var primeiro = UmDestaque(1, "Primeiro");
        var segundo = UmDestaque(2, "Segundo");
        await Semear(terceiro, primeiro, segundo);

        var ativos = await ListarAtivosDentre(terceiro, primeiro, segundo);

        ativos.Select(d => d.Titulo).Should().Equal("Primeiro", "Segundo", "Terceiro");
    }

    [Fact]
    public async Task ListarAtivosAsync_nao_devolve_destaque_desativado()
    {
        var ativo = UmDestaque(1, "Ativo");
        var desativado = UmDestaque(2, "Desativado");
        await Semear(ativo, desativado);

        // Desativa depois de gravado: Desativar() é a transição real do domínio.
        await using (var contexto = fixture.CriarContexto())
        {
            var gravado = await contexto.DestaquesHero.FindAsync(desativado.Id);
            gravado!.Desativar();
            await contexto.SaveChangesAsync();
        }

        var ativos = await ListarAtivosDentre(ativo, desativado);

        ativos.Select(d => d.Titulo).Should().Equal("Ativo");
    }

    [Fact]
    public async Task ListarAtivosAsync_nao_rastreia_os_destaques()
    {
        await Semear(UmDestaque(1));

        await using var contexto = fixture.CriarContexto();
        await new DestaqueHeroRepository(contexto).ListarAtivosAsync();

        contexto.ChangeTracker.Entries().Should().BeEmpty();
    }
}
