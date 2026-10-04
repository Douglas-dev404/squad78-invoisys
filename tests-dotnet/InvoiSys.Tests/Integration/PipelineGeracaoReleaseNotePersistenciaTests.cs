using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Tests.Fakes;
using Xunit.Abstractions;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class PipelineGeracaoReleaseNotePersistenciaTests(
    PostgresContainerFixture fixture,
    ITestOutputHelper saida)
{
    private void Etapa(string descricao) => saida.WriteLine($"\n===== {descricao} =====");

    private static HistoriaJira Historia(string chave) => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static string ChaveJiraUnica() => $"RELEASE-TESTE-{Guid.NewGuid():N}";

    [Fact]
    public async Task Release_processada_sobrevive_a_um_restart_da_aplicacao()
    {
        var chaveRelease = ChaveJiraUnica();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1"), Historia("INV-2")] };
        var llm = new FakeLlmProvider
        {
            TituloEResumo = ("Release de agosto", "Resumo da release."),
        };

        Etapa("1. Pipeline processa e o EF Core grava a Release no Postgres");
        await using (var escrita = fixture.CriarContexto(saida))
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(escrita));
            await pipeline.ExecutarAsync(chaveRelease);
        }

        Etapa("2. Contexto novo (simula restart): relê tudo do banco");
        await using var leitura = fixture.CriarContexto(saida);
        var recarregada = await new ReleaseRepository(leitura).BuscarPorChaveJiraAsync(chaveRelease);

        recarregada.Should().NotBeNull();
        recarregada!.Status.Should().Be(StatusPipeline.AguardandoRevisao);
        recarregada.Historias.Should().HaveCount(2);
        recarregada.TituloExecutivo.Should().Be("Release de agosto");
        recarregada.ResumoExecutivo.Should().Be("Resumo da release.");
        recarregada.Itens.Should().HaveCount(2);
        recarregada.Execucoes.Should().ContainSingle(
            e => e.Status == StatusExecucaoPipeline.Concluida);
    }

    [Fact]
    public async Task Reprocessar_release_existente_nao_duplica_linha_em_releases()
    {
        var chaveRelease = ChaveJiraUnica();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider();

        Guid idPrimeiraRodada;
        await using (var primeiraEscrita = fixture.CriarContexto(saida))
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(primeiraEscrita));
            idPrimeiraRodada = (await pipeline.ExecutarAsync(chaveRelease)).Id;
        }

        jira.Historias = [Historia("INV-2"), Historia("INV-3")];
        await using (var segundaEscrita = fixture.CriarContexto(saida))
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(segundaEscrita));
            await pipeline.ExecutarAsync(chaveRelease);
        }

        await using var leitura = fixture.CriarContexto(saida);
        var recarregada = await new ReleaseRepository(leitura).BuscarPorChaveJiraAsync(chaveRelease);

        recarregada.Should().NotBeNull();
        recarregada!.Id.Should().Be(idPrimeiraRodada, "reprocessar reusa o mesmo agregado");
        recarregada.Historias.Select(h => h.Chave)
            .Should().BeEquivalentTo(["INV-2", "INV-3"], "histórias refletem só a última rodada");
        recarregada.Execucoes.Should().HaveCount(2, "cada rodada preserva seu próprio rastro");
    }

    [Fact]
    public async Task Falha_no_pipeline_persiste_a_execucao_com_erro()
    {
        var chaveRelease = ChaveJiraUnica();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider { FalhaAoChamar = new InvalidOperationException("boom") };

        await using (var escrita = fixture.CriarContexto(saida))
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(escrita));
            var acao = async () => await pipeline.ExecutarAsync(chaveRelease);
            await acao.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var leitura = fixture.CriarContexto(saida);
        var recarregada = await new ReleaseRepository(leitura).BuscarPorChaveJiraAsync(chaveRelease);

        recarregada.Should().NotBeNull();
        recarregada!.Status.Should().Be(StatusPipeline.Falhou);
        recarregada.Execucoes.Should().ContainSingle(
            e => e.Status == StatusExecucaoPipeline.Falhou && e.Erro == "boom");
    }

    [Fact]
    public async Task Versao_aprovada_so_volta_a_ser_gerada_depois_de_reaberta()
    {
        var chaveRelease = ChaveJiraUnica();
        var jira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        var llm = new FakeLlmProvider { TextoReescrito = "Texto que o revisor aprovou." };

        Etapa("1. Primeiro processamento: INSERT da Release, histórias, versão, itens e execução");
        await using (var contexto = fixture.CriarContexto(saida))
        {
            await new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(contexto))
                .ExecutarAsync(chaveRelease);
        }

        Etapa("2. Revisor aprova a versão Cliente: UPDATE em versoes_comunicado e releases");
        await Revisar(chaveRelease, r => r.Aprovar("revisora@invoisys.com", DateTimeOffset.UtcNow));

        jira.Historias = [Historia("INV-2")];
        llm.TextoReescrito = "Texto novo da IA.";
        jira.ChavesConsultadas.Clear();
        Etapa("3. Reprocessar sem reabrir: só o SELECT, nenhuma escrita (recusado)");
        await using (var contexto = fixture.CriarContexto(saida))
        {
            var acao = async () => await new PipelineGeracaoReleaseNote(
                jira, llm, new ReleaseRepository(contexto)).ExecutarAsync(chaveRelease);
            await acao.Should().ThrowAsync<RevisaoHumanaObrigatoriaException>();
        }

        jira.ChavesConsultadas.Should().BeEmpty("a recusa acontece antes de qualquer chamada externa");
        var bloqueada = await Recarregar(chaveRelease);
        bloqueada.VersaoCliente!.Status.Should().Be(StatusRevisao.Aprovado);
        bloqueada.Itens.Select(i => i.Texto).Should().Equal("Texto que o revisor aprovou.");
        bloqueada.Historias.Select(h => h.Chave).Should().Equal("INV-1");
        bloqueada.Execucoes.Should().HaveCount(1, "tentativa recusada não vira execução");

        Etapa("4. Revisor reabre a revisão: UPDATE volta a versão para aguardando_revisao");
        await Revisar(chaveRelease, r => r.Reabrir());
        Etapa("5. Reprocessa: troca histórias e itens (DELETE + INSERT) e registra a 2ª execução");
        await using (var contexto = fixture.CriarContexto(saida))
        {
            await new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(contexto))
                .ExecutarAsync(chaveRelease);
        }

        Etapa("6. Relê do banco para conferir o resultado final");
        var reprocessada = await Recarregar(chaveRelease);
        reprocessada.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        reprocessada.Itens.Select(i => i.Texto).Should().Equal("Texto novo da IA.");
        reprocessada.Historias.Select(h => h.Chave).Should().Equal("INV-2");
        reprocessada.Execucoes.Should().HaveCount(2);
    }

    private async Task Revisar(string chaveRelease, Action<Release> acao)
    {
        await using var contexto = fixture.CriarContexto(saida);
        var repositorio = new ReleaseRepository(contexto);
        var release = await repositorio.BuscarPorChaveJiraAsync(chaveRelease);
        acao(release!);
        await repositorio.SalvarAsync(release!);
    }

    private async Task<Release> Recarregar(string chaveRelease)
    {
        await using var contexto = fixture.CriarContexto(saida);
        return (await new ReleaseRepository(contexto).BuscarPorChaveJiraAsync(chaveRelease))!;
    }
}
