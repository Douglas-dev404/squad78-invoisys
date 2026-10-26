using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class PipelineBloqueioTests
{
    private static HistoriaJira Historia(string chave) => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static PipelineGeracaoReleaseNote Pipeline(
        FakeJiraClient jira,
        FakeLlmProvider llm,
        FakeBloqueioProcessamento bloqueio) =>
        new(jira, llm, new FakeReleaseRepository(), modeloLlm: null, bloqueio);

    [Fact]
    public async Task Release_ja_em_processamento_e_recusada_sem_chamar_Jira_nem_LLM()
    {
        var bloqueio = new FakeBloqueioProcessamento();
        bloqueio.Ocupar("REL-1");
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider();

        var acao = async () => await Pipeline(jira, llm, bloqueio).ExecutarAsync("REL-1");

        await acao.Should().ThrowAsync<ProcessamentoEmAndamentoException>();
        jira.ChavesConsultadas.Should().BeEmpty("recusar antes de gastar chamada externa");
        llm.ChamadasAgrupar.Should().Be(0, "a segunda chamada paga à IA é exatamente o que a trava evita");
    }

    [Fact]
    public async Task Trava_e_liberada_ao_fim_da_execucao_e_permite_reprocessar()
    {
        var bloqueio = new FakeBloqueioProcessamento();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var pipeline = Pipeline(jira, new FakeLlmProvider(), bloqueio);

        await pipeline.ExecutarAsync("REL-1");

        bloqueio.EstaOcupada("REL-1").Should().BeFalse();
        await pipeline.Invoking(p => p.ExecutarAsync("REL-1")).Should().NotThrowAsync();
    }

    [Fact]
    public async Task Trava_e_liberada_mesmo_quando_o_pipeline_falha()
    {
        var bloqueio = new FakeBloqueioProcessamento();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("LLM fora do ar") };

        var acao = async () => await Pipeline(jira, llm, bloqueio).ExecutarAsync("REL-1");

        await acao.Should().ThrowAsync<InvalidOperationException>();
        bloqueio.EstaOcupada("REL-1").Should().BeFalse("senão a Release ficaria presa até reiniciar a API");
    }

    [Fact]
    public async Task Releases_diferentes_nao_se_bloqueiam()
    {
        var bloqueio = new FakeBloqueioProcessamento();
        bloqueio.Ocupar("REL-1");
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };

        var acao = async () => await Pipeline(jira, new FakeLlmProvider(), bloqueio).ExecutarAsync("REL-2");

        await acao.Should().NotThrowAsync();
    }
}
