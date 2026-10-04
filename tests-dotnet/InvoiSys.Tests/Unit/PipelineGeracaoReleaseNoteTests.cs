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
    public async Task Chave_inventada_pelo_LLM_e_ignorada_em_vez_de_derrubar_o_pipeline()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1"), Historia("INV-2")] };
        var llm = new FakeLlmProvider
        {
            GruposFixos = [["INV-1", "INV-INEXISTENTE"], ["INV-2"]],
        };

        var release = await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-1");

        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);
        release.Itens.Should().HaveCount(2, "nenhuma história real pode se perder");
        release.Itens.SelectMany(i => i.Origens)
            .Should().BeEquivalentTo(["INV-1", "INV-2"], "a chave inventada não vira origem");
    }

    [Fact]
    public async Task Release_com_versao_aprovada_nao_e_reprocessada_nem_chama_Jira_ou_LLM()
    {
        var repositorio = new FakeReleaseRepository();
        var release = await new PipelineGeracaoReleaseNote(
            new FakeJiraClient { Historias = [Historia("INV-1")] },
            new FakeLlmProvider(),
            repositorio).ExecutarAsync("REL-1");
        release.Aprovar("revisora@invoisys.com", DateTimeOffset.UtcNow);

        var jira = new FakeJiraClient { Historias = [Historia("INV-9")] };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("não chamar o LLM") };
        var acao = async () => await new PipelineGeracaoReleaseNote(jira, llm, repositorio)
            .ExecutarAsync("REL-1");

        await acao.Should().ThrowAsync<RevisaoHumanaObrigatoriaException>();
        jira.ChavesConsultadas.Should().BeEmpty("recusar antes de gastar chamada externa");
        release.Historias.Select(h => h.Chave).Should().Equal("INV-1");
        release.Execucoes.Should().HaveCount(1, "tentativa recusada não vira execução");
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.Aprovado);
    }

    [Fact]
    public async Task Grupo_inteiro_de_chaves_inventadas_nao_vira_item()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider
        {
            GruposFixos = [["INV-1"], ["INV-FANTASMA-1", "INV-FANTASMA-2"]],
        };

        var release = await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-1");

        release.Itens.Should().HaveCount(1, "um grupo só de chaves inexistentes não gera item");
    }

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

        var release = await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-1");

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

        var release = await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-1");

        release.Itens.Should().HaveCount(2);
        release.Itens[0].Origens.Should().Equal("INV-1", "INV-2");
        release.Itens[1].Origens.Should().Equal("INV-3");

        llm.GruposReescritos[0].Should().HaveCount(2);
    }

    [Fact]
    public async Task Falha_no_LLM_marca_a_release_como_falhou_e_propaga()
    {
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("boom") };

        var acao = async () =>
            await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-1");

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

        var release = await new PipelineGeracaoReleaseNote(jira, llm, new FakeReleaseRepository()).ExecutarAsync("REL-VAZIA");

        release.Itens.Should().BeEmpty();
        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);

        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);
        acao.Should().Throw<ReleaseSemItensProcessadosException>();
    }
}
