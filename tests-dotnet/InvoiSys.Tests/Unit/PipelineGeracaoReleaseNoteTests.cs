using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class PipelineGeracaoReleaseNoteTests
{
    private static HistoriaJira Historia(
        string chave,
        string descricao = "Descrição técnica",
        string? releaseNote = null) => new()
        {
            Chave = chave,
            Titulo = "Título",
            DescricaoTecnica = descricao,
            TipoIssue = "Story",
            TextoReleaseNote = releaseNote,
        };

    [Fact]
    public async Task Pipeline_completo_deixa_a_release_aguardando_revisao()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1"), Historia("INV-2")] };
        var llm = new FakeLlmProvider
        {
            CategoriaFixa = CategoriaAlteracao.NovaFuncionalidade,
            TextoReescrito = "Agora é possível emitir notas em lote.",
            TituloEResumo = ("Release de agosto", "Resumo da release."),
        };

        var release = await new PipelineGeracaoReleaseNote(jira, llm).ExecutarAsync("REL-1");

        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);
        release.ProntaParaExportar.Should().BeFalse("publicação exige aprovação humana");
        release.TituloExecutivo.Should().Be("Release de agosto");
        release.ResumoExecutivo.Should().Be("Resumo da release.");
        release.Itens.Should().HaveCount(2);
        release.Itens.Should().AllSatisfy(i =>
            i.Categoria.Should().Be(CategoriaAlteracao.NovaFuncionalidade));
        jira.ChavesConsultadas.Should().ContainSingle().Which.Should().Be("REL-1");
    }

    [Fact]
    public async Task Historias_agrupadas_viram_um_unico_item_com_todas_as_origens()
    {
        var jira = new FakeJiraClient
        {
            Historias = [Historia("INV-1"), Historia("INV-2"), Historia("INV-3")],
        };
        var llm = new FakeLlmProvider
        {
            GruposFixos = [["INV-1", "INV-2"], ["INV-3"]],
        };

        var release = await new PipelineGeracaoReleaseNote(jira, llm).ExecutarAsync("REL-1");

        release.Itens.Should().HaveCount(2);
        release.Itens[0].Origens.Should().Equal("INV-1", "INV-2");
        release.Itens[1].Origens.Should().Equal("INV-3");

        // O grupo fundido manda os dois textos juntos para a reescrita — é isso que
        // elimina a duplicata no comunicado final.
        llm.GruposReescritos[0].Should().HaveCount(2);
    }

    [Fact]
    public async Task Falha_no_LLM_marca_a_release_como_falhou_e_propaga()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("boom") };

        var acao = async () =>
            await new PipelineGeracaoReleaseNote(jira, llm).ExecutarAsync("REL-1");

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public void Estagio_1_normaliza_a_descricao_tecnica_quando_nao_ha_release_note()
    {
        var historia = Historia("INV-1", descricao: "Texto   com\n\nespaços\tbagunçados");

        var limpa = PipelineGeracaoReleaseNote.ExtrairELimpar(historia);

        limpa.DescricaoTecnica.Should().Be("Texto com espaços bagunçados");
        limpa.TextoFonte.Should().Be("Texto com espaços bagunçados");
    }

    [Fact]
    public void Estagio_1_normaliza_a_release_note_quando_ela_existe()
    {
        // Este é o caso que um "sempre limpar DescricaoTecnica" quebraria em silêncio:
        // TextoFonte devolveria a Release Note original, suja, ignorando a limpeza.
        var historia = Historia(
            "INV-1",
            descricao: "descrição técnica",
            releaseNote: "Release   note\n\ncom  espaços");

        var limpa = PipelineGeracaoReleaseNote.ExtrairELimpar(historia);

        limpa.TextoReleaseNote.Should().Be("Release note com espaços");
        limpa.TextoFonte.Should().Be("Release note com espaços");
        limpa.DescricaoTecnica.Should().Be(
            "descrição técnica",
            "a descrição original é preservada quando não é a fonte");
    }

    [Fact]
    public async Task Release_sem_historias_conclui_sem_itens()
    {
        var jira = new FakeJiraClient { Historias = [] };
        var llm = new FakeLlmProvider();

        var release = await new PipelineGeracaoReleaseNote(jira, llm).ExecutarAsync("REL-VAZIA");

        release.Itens.Should().BeEmpty();
        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);

        // E o gate segue valendo: sem itens, não há o que aprovar.
        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);
        acao.Should().Throw<ReleaseSemItensProcessadosException>();
    }
}
