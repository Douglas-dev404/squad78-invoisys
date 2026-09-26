using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração de <see cref="ExecucaoPipelineRepository"/> contra Postgres real.
/// As execuções são gravadas pelo caminho de produção — <see cref="Release.RegistrarExecucao"/>
/// + <see cref="ReleaseRepository"/> —, este repository só lê.
/// </summary>
[Collection("Postgres")]
public class ExecucaoPipelineRepositoryTests(PostgresContainerFixture fixture)
{
    private async Task<Release> SalvarReleaseComExecucoes(params Action<ExecucaoPipeline>[] desfechos)
    {
        var release = new Release($"RELEASE-TESTE-{Guid.NewGuid():N}", []);
        foreach (var desfecho in desfechos)
        {
            desfecho(release.RegistrarExecucao("openai/gpt-4o-mini"));
            // Garante IniciadoEm distinto entre execuções para a ordenação ser verificável.
            await Task.Delay(5);
        }

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);

        return release;
    }

    [Fact]
    public async Task ListarPorReleaseAsync_devolve_da_mais_recente_para_a_mais_antiga()
    {
        var release = await SalvarReleaseComExecucoes(
            e => e.MarcarFalha("timeout"),
            e => e.MarcarConcluida());
        await SalvarReleaseComExecucoes(e => e.MarcarConcluida());

        await using var contexto = fixture.CriarContexto();
        var execucoes = await new ExecucaoPipelineRepository(contexto).ListarPorReleaseAsync(release.Id);

        execucoes.Select(e => e.Status)
            .Should().Equal(StatusExecucaoPipeline.Concluida, StatusExecucaoPipeline.Falhou);
        execucoes[1].Erro.Should().Be("timeout");
        contexto.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ListarPorReleaseAsync_devolve_vazio_para_release_sem_execucao_ou_inexistente()
    {
        var semExecucao = await SalvarReleaseComExecucoes();

        await using var contexto = fixture.CriarContexto();
        var repositorio = new ExecucaoPipelineRepository(contexto);

        (await repositorio.ListarPorReleaseAsync(semExecucao.Id)).Should().BeEmpty();
        (await repositorio.ListarPorReleaseAsync(Guid.NewGuid())).Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarPorIdAsync_devolve_a_execucao_ou_null()
    {
        var release = await SalvarReleaseComExecucoes(e => e.MarcarConcluida());
        var id = release.Execucoes.Single().Id;

        await using var contexto = fixture.CriarContexto();
        var repositorio = new ExecucaoPipelineRepository(contexto);

        var encontrada = await repositorio.BuscarPorIdAsync(id);
        encontrada!.ReleaseId.Should().Be(release.Id);
        encontrada.ModeloLlm.Should().Be("openai/gpt-4o-mini");
        (await repositorio.BuscarPorIdAsync(Guid.NewGuid())).Should().BeNull();
    }
}
