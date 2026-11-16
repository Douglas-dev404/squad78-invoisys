using System.Text.Json;
using FluentAssertions;
using InvoiSys.Application.Consulta;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class QuadroReleasesTests
{
    private static HistoriaJira Historia(string chave) => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição",
        TipoIssue = "Story",
    };

    private static async Task<FakeReleaseRepository> ComReleaseProcessada(string chave)
    {
        var repositorio = new FakeReleaseRepository();
        var release = new Release(chave, [Historia("INV-1")]);
        release.ConcluirProcessamento([new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto.", ["INV-1"])], "T", "R");
        await repositorio.SalvarAsync(release);
        return repositorio;
    }

    [Fact]
    public async Task Junta_versoes_do_Jira_com_o_status_no_InvoiSys()
    {
        var jira = new FakeJiraClient
        {
            Versoes =
            [
                new VersaoJira("2026.11.0", "Novembro", Lancada: false, Arquivada: false, new DateOnly(2026, 11, 30)),
                new VersaoJira("2026.10.0", null, Lancada: true, Arquivada: false, null),
            ],
        };
        var repositorio = await ComReleaseProcessada("2026.10.0");

        var quadro = await new QuadroReleases(jira, repositorio).MontarAsync();

        quadro.AvisoJira.Should().BeNull();
        quadro.Cartoes.Should().HaveCount(2);
        var nova = quadro.Cartoes.Single(c => c.Chave == "2026.11.0");
        nova.Status.Should().BeNull("ainda não foi processada");
        nova.NoJira.Should().BeTrue();
        nova.DataLancamento.Should().Be(new DateOnly(2026, 11, 30));
        quadro.Cartoes.Single(c => c.Chave == "2026.10.0").Status.Should().Be(StatusPipeline.AguardandoRevisao);
    }

    [Fact]
    public async Task Versao_arquivada_some_e_release_processada_fora_do_Jira_continua_no_quadro()
    {
        var jira = new FakeJiraClient
        {
            Versoes = [new VersaoJira("2025.01.0", null, Lancada: true, Arquivada: true, null)],
        };
        var repositorio = await ComReleaseProcessada("ANTIGA-1");

        var quadro = await new QuadroReleases(jira, repositorio).MontarAsync();

        quadro.Cartoes.Should().ContainSingle();
        var antiga = quadro.Cartoes[0];
        antiga.Chave.Should().Be("ANTIGA-1");
        antiga.NoJira.Should().BeFalse();
    }

    [Fact]
    public async Task Jira_fora_do_ar_ou_sem_configuracao_devolve_so_as_processadas_com_aviso()
    {
        var repositorio = await ComReleaseProcessada("2026.10.0");

        foreach (Exception falha in new Exception[]
        {
            new ProviderNaoConfiguradoException("Jira não configurado"),
            new JiraApiException("Jira retornou 503."),
        })
        {
            var jira = new FakeJiraClient { FalhaAoListarVersoes = falha };

            var quadro = await new QuadroReleases(jira, repositorio).MontarAsync();

            quadro.AvisoJira.Should().Be(falha.Message);
            quadro.Cartoes.Select(c => c.Chave).Should().Equal("2026.10.0");
        }
    }

    [Fact]
    public void Converte_versao_da_API_do_Jira()
    {
        var json = JsonDocument.Parse("""
            { "id": "10001", "name": " 2026.11.0 ", "description": "Novembro", "released": false,
              "archived": false, "releaseDate": "2026-11-30", "projectId": 10000 }
            """).RootElement;

        JiraRestClient.ParaVersaoJira(json).Should().Be(
            new VersaoJira("2026.11.0", "Novembro", false, false, new DateOnly(2026, 11, 30)));

        JiraRestClient.ParaVersaoJira(JsonDocument.Parse("""{ "id": "1" }""").RootElement).Should().BeNull();
    }
}
