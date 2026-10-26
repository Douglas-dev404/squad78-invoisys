using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Observabilidade;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class TelemetriaTests
{
    private static ActivityListener ListenerComAmostragemPorPai() => new()
    {
        ShouldListenTo = fonte => fonte.Name == Telemetria.Nome,
        Sample = (ref ActivityCreationOptions<ActivityContext> opcoes) =>
            opcoes.Parent.TraceId != default && !opcoes.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                ? ActivitySamplingResult.None
                : ActivitySamplingResult.AllDataAndRecorded,
    };

    [Fact]
    public void Span_filho_de_Activity_nao_amostrada_some_mas_a_raiz_explicita_nao()
    {
        using var listener = ListenerComAmostragemPorPai();
        ActivitySource.AddActivityListener(listener);

        using var requisicao = new Activity("requisicao-http").Start();
        requisicao.Recorded.Should().BeFalse("é o que o ASP.NET cria só para correlacionar logs");

        using var filho = Telemetria.Atividades.StartActivity("pipeline");
        filho.Should().BeNull("herda o 'não amostrado' do pai — a armadilha que escondeu os traces");

        using var raiz = Telemetria.IniciarRaiz("pipeline");
        raiz.Should().NotBeNull();
        raiz!.Recorded.Should().BeTrue();
        raiz.Parent.Should().BeNull();
    }

    [Fact]
    public async Task Pipeline_gera_trace_de_raiz_com_o_nome_do_trace_do_Langfuse_mesmo_dentro_de_requisicao()
    {
        var gravadas = new ConcurrentBag<Activity>();
        using var listener = ListenerComAmostragemPorPai();
        listener.ActivityStopped = gravadas.Add;
        ActivitySource.AddActivityListener(listener);

        var jira = new FakeJiraClient
        {
            Historias = [new HistoriaJira { Chave = "INV-1", Titulo = "T", DescricaoTecnica = "D", TipoIssue = "Story" }],
        };

        using var requisicao = new Activity("requisicao-http").Start();
        await new PipelineGeracaoReleaseNote(
            jira, new FakeLlmProvider(), new FakeReleaseRepository(), new FakeBloqueioProcessamento())
            .ExecutarAsync("REL-TELEMETRIA");

        var raiz = gravadas
            .Where(a => a.OperationName == "pipeline.gerar-release-note"
                && (string?)a.GetTagItem("invoisys.release.chave") == "REL-TELEMETRIA")
            .Should().ContainSingle().Subject;
        raiz.Parent.Should().BeNull();
        raiz.GetTagItem("langfuse.trace.name").Should().Be("gerar-release-note");
        raiz.GetTagItem("invoisys.release.chave").Should().Be("REL-TELEMETRIA");
    }

    [Fact]
    public async Task Execucao_barrada_pela_trava_incrementa_a_metrica_de_bloqueios()
    {
        long barradas = 0;
        using var medidor = new MeterListener();
        medidor.InstrumentPublished = (instrumento, ouvinte) =>
        {
            if (instrumento.Meter.Name == Telemetria.Nome && instrumento.Name == "invoisys.pipeline.bloqueios")
            {
                ouvinte.EnableMeasurementEvents(instrumento);
            }
        };
        medidor.SetMeasurementEventCallback<long>((_, valor, _, _) => Interlocked.Add(ref barradas, valor));
        medidor.Start();

        var bloqueio = new FakeBloqueioProcessamento();
        bloqueio.Ocupar("REL-1");

        var acao = async () => await new PipelineGeracaoReleaseNote(
            new FakeJiraClient(), new FakeLlmProvider(), new FakeReleaseRepository(), bloqueio)
            .ExecutarAsync("REL-1");

        await acao.Should().ThrowAsync<ProcessamentoEmAndamentoException>();

        Interlocked.Read(ref barradas).Should().BeGreaterThanOrEqualTo(1);
    }
}
