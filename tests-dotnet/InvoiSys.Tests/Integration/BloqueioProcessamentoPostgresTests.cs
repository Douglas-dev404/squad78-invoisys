using FluentAssertions;
using InvoiSys.Application.Pipeline;
using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Tests.Fakes;
using Npgsql;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class BloqueioProcessamentoPostgresTests(PostgresContainerFixture fixture)
{
    private static HistoriaJira Historia(string chave) => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static string ChaveUnica() => $"RELEASE-TRAVA-{Guid.NewGuid():N}";

    [Fact]
    public async Task Segunda_execucao_simultanea_da_mesma_Release_e_barrada_antes_de_chamar_a_IA()
    {
        var chave = ChaveUnica();
        await using var fonte = NpgsqlDataSource.Create(fixture.ConnectionString);
        var bloqueio = new PostgresBloqueioProcessamento(fonte);

        var llmPrimeira = new FakeLlmProvider { Portao = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var jiraPrimeira = new FakeJiraClient { Historias = [Historia("INV-1")] };
        await using var contextoPrimeira = fixture.CriarContexto();
        var primeira = new PipelineGeracaoReleaseNote(
            jiraPrimeira, llmPrimeira, new ReleaseRepository(contextoPrimeira), null, bloqueio);

        var execucaoPrimeira = primeira.ExecutarAsync(chave);
        await llmPrimeira.IniciouAgrupar.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var llmSegunda = new FakeLlmProvider();
        var jiraSegunda = new FakeJiraClient { Historias = [Historia("INV-1")] };
        await using var contextoSegunda = fixture.CriarContexto();
        var segunda = new PipelineGeracaoReleaseNote(
            jiraSegunda, llmSegunda, new ReleaseRepository(contextoSegunda), null, bloqueio);

        var acaoSegunda = async () => await segunda.ExecutarAsync(chave);

        await acaoSegunda.Should().ThrowAsync<ProcessamentoEmAndamentoException>();
        llmSegunda.ChamadasAgrupar.Should().Be(0);
        jiraSegunda.ChavesConsultadas.Should().BeEmpty();

        llmPrimeira.Portao!.SetResult();
        var release = await execucaoPrimeira.WaitAsync(TimeSpan.FromSeconds(10));
        release.ChaveJira.Should().Be(chave);
        llmPrimeira.ChamadasAgrupar.Should().Be(1);
    }

    [Fact]
    public async Task Trava_e_liberada_quando_a_execucao_termina()
    {
        var chave = ChaveUnica();
        await using var fonte = NpgsqlDataSource.Create(fixture.ConnectionString);
        var bloqueio = new PostgresBloqueioProcessamento(fonte);

        var primeira = await bloqueio.TentarAdquirirAsync(chave);
        primeira.Should().NotBeNull();

        (await bloqueio.TentarAdquirirAsync(chave)).Should().BeNull("a primeira ainda segura a trava");

        await primeira!.DisposeAsync();

        var terceira = await bloqueio.TentarAdquirirAsync(chave);
        terceira.Should().NotBeNull("liberar a trava devolve a Release");
        await terceira!.DisposeAsync();
    }

    [Fact]
    public async Task Release_diferente_nao_e_bloqueada_pela_trava_de_outra()
    {
        await using var fonte = NpgsqlDataSource.Create(fixture.ConnectionString);
        var bloqueio = new PostgresBloqueioProcessamento(fonte);

        var a = await bloqueio.TentarAdquirirAsync(ChaveUnica());
        var b = await bloqueio.TentarAdquirirAsync(ChaveUnica());

        a.Should().NotBeNull();
        b.Should().NotBeNull();
        await a!.DisposeAsync();
        await b!.DisposeAsync();
    }
}
