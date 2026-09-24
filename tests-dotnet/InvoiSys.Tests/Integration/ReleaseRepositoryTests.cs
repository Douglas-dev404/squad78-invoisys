using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração de <see cref="ReleaseRepository"/> contra Postgres real
/// (Testcontainers). Cobrem o agregado completo — Release + HistoriaJira, incluindo o
/// array nativo <c>Labels</c> — e as constraints de banco (índice único composto,
/// cascade delete) que um provider fake não validaria.
///
/// Não existe repository de HistoriaJira: ela é filha do agregado Release e é
/// persistida/carregada por este repository (ver docstring de IReleaseRepository).
/// </summary>
[Collection("Postgres")]
public class ReleaseRepositoryTests(PostgresContainerFixture fixture)
{
    private static HistoriaJira UmaHistoria(
        string chave = "INV-1",
        string? textoReleaseNote = null,
        IReadOnlyList<string>? labels = null)
    {
        return new HistoriaJira
        {
            Chave = chave,
            Titulo = "Título",
            DescricaoTecnica = "Descrição técnica",
            TipoIssue = "Story",
            TextoReleaseNote = textoReleaseNote,
            Labels = labels ?? [],
        };
    }

    // ChaveJira tem índice único global: sufixo aleatório evita colisão entre testes,
    // sem precisar resetar o banco (cada teste só toca nos dados que ele mesmo criou).
    private static string ChaveJiraUnica() => $"RELEASE-TESTE-{Guid.NewGuid():N}";

    [Fact]
    public async Task SalvarAsync_persiste_release_com_historias_e_a_releitura_traz_tudo_de_volta()
    {
        var historias = new List<HistoriaJira>
        {
            UmaHistoria("INV-1", textoReleaseNote: "Release note dedicada", labels: ["backend", "urgente"]),
            UmaHistoria("INV-2"),
        };
        var release = new Release(ChaveJiraUnica(), historias);

        await using (var escrita = fixture.CriarContexto())
        {
            await new ReleaseRepository(escrita).SalvarAsync(release);
        }

        // Contexto novo: a releitura vai de fato ao banco, não ao change tracker de quem salvou.
        await using var leitura = fixture.CriarContexto();
        var recarregada = await new ReleaseRepository(leitura).BuscarPorIdAsync(release.Id);

        recarregada.Should().NotBeNull();
        recarregada!.ChaveJira.Should().Be(release.ChaveJira);
        recarregada.Historias.Should().HaveCount(2);

        var comLabels = recarregada.Historias.Single(h => h.Chave == "INV-1");
        comLabels.Titulo.Should().Be("Título");
        comLabels.DescricaoTecnica.Should().Be("Descrição técnica");
        comLabels.TipoIssue.Should().Be("Story");
        comLabels.TextoReleaseNote.Should().Be("Release note dedicada");
        comLabels.Labels.Should().Equal("backend", "urgente");

        var semLabels = recarregada.Historias.Single(h => h.Chave == "INV-2");
        semLabels.TextoReleaseNote.Should().BeNull();
        semLabels.Labels.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarPorChaveJiraAsync_encontra_pela_chave_de_negocio_e_carrega_as_historias()
    {
        var release = new Release(ChaveJiraUnica(), [UmaHistoria("INV-1"), UmaHistoria("INV-2")]);

        await using (var escrita = fixture.CriarContexto())
        {
            await new ReleaseRepository(escrita).SalvarAsync(release);
        }

        await using var leitura = fixture.CriarContexto();
        var recarregada = await new ReleaseRepository(leitura).BuscarPorChaveJiraAsync(release.ChaveJira);

        recarregada.Should().NotBeNull();
        recarregada!.Id.Should().Be(release.Id);
        recarregada.Historias.Select(h => h.Chave).Should().BeEquivalentTo(new[] { "INV-1", "INV-2" });
    }

    [Fact]
    public async Task BuscarPorIdAsync_e_BuscarPorChaveJiraAsync_devolvem_null_quando_nao_existe()
    {
        await using var contexto = fixture.CriarContexto();
        var repositorio = new ReleaseRepository(contexto);

        var porId = await repositorio.BuscarPorIdAsync(Guid.NewGuid());
        var porChave = await repositorio.BuscarPorChaveJiraAsync(ChaveJiraUnica());

        porId.Should().BeNull();
        porChave.Should().BeNull();
    }
}
