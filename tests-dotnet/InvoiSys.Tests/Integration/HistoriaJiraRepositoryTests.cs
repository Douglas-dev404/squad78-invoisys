using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração de <see cref="HistoriaJiraRepository"/> contra Postgres real
/// (Testcontainers). Os dados são semeados via <see cref="ReleaseRepository"/> — é ele que
/// grava o agregado; este repository só lê.
/// </summary>
[Collection("Postgres")]
public class HistoriaJiraRepositoryTests(PostgresContainerFixture fixture)
{
    private static HistoriaJira UmaHistoria(
        string chave = "INV-1",
        string titulo = "Título",
        string? textoReleaseNote = null,
        IReadOnlyList<string>? labels = null)
    {
        return new HistoriaJira
        {
            Chave = chave,
            Titulo = titulo,
            DescricaoTecnica = "Descrição técnica",
            TipoIssue = "Story",
            TextoReleaseNote = textoReleaseNote,
            Labels = labels ?? [],
        };
    }

    private static string ChaveJiraUnica() => $"RELEASE-TESTE-{Guid.NewGuid():N}";

    private async Task<Release> SalvarRelease(params HistoriaJira[] historias)
    {
        var release = new Release(ChaveJiraUnica(), historias);

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);

        return release;
    }

    [Fact]
    public async Task BuscarPorIdAsync_devolve_a_historia_com_todos_os_campos()
    {
        var historia = UmaHistoria("INV-1", textoReleaseNote: "Release note dedicada", labels: ["backend", "urgente"]);
        await SalvarRelease(historia);

        await using var contexto = fixture.CriarContexto();
        var encontrada = await new HistoriaJiraRepository(contexto).BuscarPorIdAsync(historia.Id);

        encontrada.Should().NotBeNull();
        encontrada!.Chave.Should().Be("INV-1");
        encontrada.Titulo.Should().Be("Título");
        encontrada.DescricaoTecnica.Should().Be("Descrição técnica");
        encontrada.TipoIssue.Should().Be("Story");
        encontrada.TextoReleaseNote.Should().Be("Release note dedicada");
        encontrada.Labels.Should().Equal("backend", "urgente");
    }

    [Fact]
    public async Task BuscarPorIdAsync_devolve_null_quando_nao_existe()
    {
        await using var contexto = fixture.CriarContexto();

        var encontrada = await new HistoriaJiraRepository(contexto).BuscarPorIdAsync(Guid.NewGuid());

        encontrada.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorChaveAsync_nao_confunde_a_mesma_chave_de_releases_diferentes()
    {
        // A chave só é única por Release: INV-1 existe nas duas, com títulos distintos.
        var primeira = await SalvarRelease(UmaHistoria("INV-1", titulo: "Da primeira"));
        var segunda = await SalvarRelease(UmaHistoria("INV-1", titulo: "Da segunda"));

        await using var contexto = fixture.CriarContexto();
        var repositorio = new HistoriaJiraRepository(contexto);

        var daPrimeira = await repositorio.BuscarPorChaveAsync(primeira.Id, "INV-1");
        var daSegunda = await repositorio.BuscarPorChaveAsync(segunda.Id, "INV-1");

        daPrimeira!.Titulo.Should().Be("Da primeira");
        daSegunda!.Titulo.Should().Be("Da segunda");
    }

    [Fact]
    public async Task BuscarPorChaveAsync_devolve_null_quando_a_release_nao_tem_a_chave()
    {
        var release = await SalvarRelease(UmaHistoria("INV-1"));

        await using var contexto = fixture.CriarContexto();
        var repositorio = new HistoriaJiraRepository(contexto);

        var chaveInexistente = await repositorio.BuscarPorChaveAsync(release.Id, "INV-999");
        var releaseInexistente = await repositorio.BuscarPorChaveAsync(Guid.NewGuid(), "INV-1");

        chaveInexistente.Should().BeNull();
        releaseInexistente.Should().BeNull();
    }

    [Fact]
    public async Task ListarPorReleaseAsync_devolve_so_as_historias_da_release_ordenadas_pela_chave()
    {
        var release = await SalvarRelease(UmaHistoria("INV-2"), UmaHistoria("INV-1"), UmaHistoria("INV-3"));
        await SalvarRelease(UmaHistoria("INV-9"));

        await using var contexto = fixture.CriarContexto();
        var historias = await new HistoriaJiraRepository(contexto).ListarPorReleaseAsync(release.Id);

        historias.Select(h => h.Chave).Should().Equal("INV-1", "INV-2", "INV-3");
    }

    [Fact]
    public async Task ListarPorReleaseAsync_devolve_lista_vazia_quando_a_release_nao_existe()
    {
        await using var contexto = fixture.CriarContexto();

        var historias = await new HistoriaJiraRepository(contexto).ListarPorReleaseAsync(Guid.NewGuid());

        historias.Should().BeEmpty();
    }

    [Fact]
    public async Task As_consultas_nao_rastreiam_as_historias_nem_carregam_o_agregado()
    {
        var release = await SalvarRelease(UmaHistoria("INV-1"), UmaHistoria("INV-2"));

        await using var contexto = fixture.CriarContexto();
        var repositorio = new HistoriaJiraRepository(contexto);

        await repositorio.ListarPorReleaseAsync(release.Id);
        await repositorio.BuscarPorChaveAsync(release.Id, "INV-1");

        // Sem tracking: nenhuma História rastreada e, principalmente, nenhuma Release
        // materializada — é a diferença para IReleaseRepository, que carrega o agregado.
        contexto.ChangeTracker.Entries().Should().BeEmpty();
    }
}
