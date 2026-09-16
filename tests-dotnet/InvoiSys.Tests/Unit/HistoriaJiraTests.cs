using FluentAssertions;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// A regra de precedência da fonte de texto: Release Note dedicada ganha da descrição
/// técnica. É o que decide o que alimenta o pipeline de IA.
/// </summary>
public class HistoriaJiraTests
{
    private static HistoriaJira Historia(string? releaseNote) => new()
    {
        Chave = "INV-1234",
        Titulo = "Emissão de NFS-e",
        DescricaoTecnica = "Refatorado o serviço de emissão para usar fila assíncrona.",
        TipoIssue = "Story",
        TextoReleaseNote = releaseNote,
    };

    [Fact]
    public void Sem_release_note_o_texto_fonte_e_a_descricao_tecnica()
    {
        var historia = Historia(null);

        historia.PossuiReleaseNoteDedicada.Should().BeFalse();
        historia.TextoFonte.Should().Be(historia.DescricaoTecnica);
    }

    [Fact]
    public void Com_release_note_o_texto_fonte_e_a_release_note()
    {
        var historia = Historia("Agora a emissão de notas é mais rápida.");

        historia.PossuiReleaseNoteDedicada.Should().BeTrue();
        historia.TextoFonte.Should().Be("Agora a emissão de notas é mais rápida.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void Release_note_vazia_ou_so_com_espacos_nao_conta_como_dedicada(string vazio)
    {
        var historia = Historia(vazio);

        historia.PossuiReleaseNoteDedicada.Should().BeFalse();
        historia.TextoFonte.Should().Be(historia.DescricaoTecnica);
    }
}
