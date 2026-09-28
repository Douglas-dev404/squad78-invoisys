using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração da persistência feita por <see cref="PipelineGeracaoReleaseNote"/>
/// contra Postgres real (Testcontainers). Jira e LLM continuam fakes — o que este teste
/// cobre é só o comportamento novo: gravar via <see cref="ReleaseRepository"/> ao fim do
/// pipeline, inclusive no caminho de falha, e não duplicar linha ao reprocessar.
/// </summary>
[Collection("Postgres")]
public class PipelineGeracaoReleaseNotePersistenciaTests(PostgresContainerFixture fixture)
{
    private static HistoriaJira Historia(string chave) => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    // ChaveJira tem índice único global: sufixo aleatório evita colisão entre testes,
    // sem precisar resetar o banco (mesmo helper de ReleaseRepositoryTests).
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

        await using (var escrita = fixture.CriarContexto())
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(escrita));
            await pipeline.ExecutarAsync(chaveRelease);
        }

        // Contexto novo: simula o restart, a releitura vai de fato ao banco.
        await using var leitura = fixture.CriarContexto();
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
        await using (var primeiraEscrita = fixture.CriarContexto())
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(primeiraEscrita));
            idPrimeiraRodada = (await pipeline.ExecutarAsync(chaveRelease)).Id;
        }

        jira.Historias = [Historia("INV-2"), Historia("INV-3")];
        await using (var segundaEscrita = fixture.CriarContexto())
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(segundaEscrita));
            await pipeline.ExecutarAsync(chaveRelease);
        }

        await using var leitura = fixture.CriarContexto();
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

        await using (var escrita = fixture.CriarContexto())
        {
            var pipeline = new PipelineGeracaoReleaseNote(jira, llm, new ReleaseRepository(escrita));
            var acao = async () => await pipeline.ExecutarAsync(chaveRelease);
            await acao.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var leitura = fixture.CriarContexto();
        var recarregada = await new ReleaseRepository(leitura).BuscarPorChaveJiraAsync(chaveRelease);

        recarregada.Should().NotBeNull();
        recarregada!.Status.Should().Be(StatusPipeline.Falhou);
        recarregada.Execucoes.Should().ContainSingle(
            e => e.Status == StatusExecucaoPipeline.Falhou && e.Erro == "boom");
    }
}
