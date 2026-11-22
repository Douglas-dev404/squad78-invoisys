using System.Text.Json;
using FluentAssertions;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class ConsultaVersoesJiraTests
{
    private static VersaoJira Versao(string nome, bool lancada = false, string? data = null, bool arquivada = false) =>
        new(nome, null, data is null ? null : DateOnly.Parse(data), lancada, arquivada);

    private static async Task<FakeReleaseRepository> ComRelease(string chave, bool aprovar = false)
    {
        var repositorio = new FakeReleaseRepository();
        var release = new Release(chave, [new HistoriaJira { Chave = "INV-1", Titulo = "T", DescricaoTecnica = "D", TipoIssue = "Story" }]);
        release.ConcluirProcessamento([new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto.", ["INV-1"])], "T", "R");
        if (aprovar)
        {
            release.Aprovar(AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);
        }

        await repositorio.SalvarAsync(release);
        return repositorio;
    }

    [Fact]
    public async Task Junta_as_versoes_do_Jira_com_o_status_do_banco_numa_consulta_so()
    {
        var jira = new FakeJiraClient { Versoes = [Versao("2026.11.0"), Versao("2026.10.0", lancada: true)] };
        var repositorio = await ComRelease("2026.10.0");

        var versoes = await new ConsultaVersoesJira(jira, repositorio).ListarAsync(incluirLancadas: true);

        versoes.Single(v => v.Versao.Nome == "2026.10.0").StatusInvoisys.Should().Be(StatusPipeline.AguardandoRevisao);
        versoes.Single(v => v.Versao.Nome == "2026.11.0").StatusInvoisys.Should().BeNull("ainda não foi processada");
        repositorio.ConsultasDeStatus.Should().Be(1);
    }

    [Fact]
    public async Task Arquivadas_ficam_de_fora_e_incluirLancadas_false_traz_so_as_nao_lancadas()
    {
        var jira = new FakeJiraClient
        {
            Versoes = [Versao("2026.11.0"), Versao("2026.10.0", lancada: true), Versao("2025.01.0", arquivada: true)],
        };

        var todas = await new ConsultaVersoesJira(jira, new FakeReleaseRepository()).ListarAsync(incluirLancadas: true);
        var naoLancadas = await new ConsultaVersoesJira(jira, new FakeReleaseRepository()).ListarAsync(incluirLancadas: false);

        todas.Select(v => v.Versao.Nome).Should().BeEquivalentTo(["2026.11.0", "2026.10.0"]);
        naoLancadas.Select(v => v.Versao.Nome).Should().Equal("2026.11.0");
    }

    [Fact]
    public async Task Ordem_nao_lancadas_primeiro_depois_data_de_lancamento_decrescente()
    {
        var jira = new FakeJiraClient
        {
            Versoes =
            [
                Versao("2026.09.0", lancada: true, data: "2026-09-30"),
                Versao("2026.12.0", data: "2026-12-18"),
                Versao("2026.10.0", lancada: true, data: "2026-10-31"),
                Versao("sem-data"),
                Versao("2026.11.0", data: "2026-11-27"),
            ],
        };

        var versoes = await new ConsultaVersoesJira(jira, new FakeReleaseRepository()).ListarAsync(incluirLancadas: true);

        versoes.Select(v => v.Versao.Nome).Should().Equal("2026.12.0", "2026.11.0", "sem-data", "2026.10.0", "2026.09.0");
    }

    [Fact]
    public void Converte_versao_da_API_do_Jira_mantendo_o_nome_literal_e_ignora_versao_sem_nome()
    {
        var json = JsonDocument.Parse("""
            { "id": "10001", "name": "2026.10.0", "description": "Release de outubro", "released": true,
              "archived": false, "releaseDate": "2026-10-31", "projectId": 10000 }
            """).RootElement;

        JiraRestClient.ParaVersaoJira(json).Should().Be(
            new VersaoJira("2026.10.0", "Release de outubro", new DateOnly(2026, 10, 31), true, false));
        JiraRestClient.ParaVersaoJira(JsonDocument.Parse("""{ "id": "1", "description": "" }""").RootElement)
            .Should().BeNull();
        JiraRestClient.ParaVersaoJira(JsonDocument.Parse("""{ "id": "2", "name": "Sprint 3 " }""").RootElement)!
            .Nome.Should().Be("Sprint 3 ");
    }
}
