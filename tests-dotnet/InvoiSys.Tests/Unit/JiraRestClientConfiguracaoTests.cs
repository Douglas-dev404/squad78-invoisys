using System.Web;
using FluentAssertions;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Jira;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiSys.Tests.Unit;

public class JiraRestClientConfiguracaoTests
{
    private static (JiraRestClient Cliente, FakeHttpMessageHandler Handler) Criar(
        string projeto = "INV",
        string tipoSubtarefa = "Release Note",
        string campoCustomizado = "")
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://teste.atlassian.net") };
        var opcoes = Options.Create(new JiraOptions
        {
            BaseUrl = "https://teste.atlassian.net",
            Email = "teste@exemplo.com",
            ApiToken = "token-de-teste",
            Projeto = projeto,
            ReleaseNote = new JiraReleaseNoteOptions { TipoSubtarefa = tipoSubtarefa, CampoCustomizado = campoCustomizado },
        });
        return (new JiraRestClient(http, opcoes, NullLogger<JiraRestClient>.Instance), handler);
    }

    private static string Busca(params string[] issues) =>
        $$"""{ "issues": [{{string.Join(",", issues)}}], "isLast": true }""";

    private static string JqlEnviado(FakeHttpMessageHandler handler) =>
        HttpUtility.ParseQueryString(handler.UrisChamadas[0]!.Query)["jql"]!;

    private static string CamposEnviados(FakeHttpMessageHandler handler) =>
        HttpUtility.ParseQueryString(handler.UrisChamadas[0]!.Query)["fields"]!;

    [Fact]
    public async Task Busca_filtra_pelo_projeto_alem_da_versao()
    {
        var (cliente, handler) = Criar(projeto: "INV");
        handler.ResponderJson(Busca());

        await cliente.BuscarHistoriasDaReleaseAsync("2026.10.0");

        JqlEnviado(handler).Should().Be("project = \"INV\" AND fixVersion = \"2026.10.0\"",
            "versão com o mesmo nome em outro projeto não pode entrar na Release");
    }

    [Theory]
    [InlineData("2026.10.0\" OR project = \"OUTRO", "project = \"INV\" AND fixVersion = \"2026.10.0\\\" OR project = \\\"OUTRO\"")]
    [InlineData("versao\\\" OR 1=1", "project = \"INV\" AND fixVersion = \"versao\\\\\\\" OR 1=1\"")]
    [InlineData("1.0 \\ beta", "project = \"INV\" AND fixVersion = \"1.0 \\\\ beta\"")]
    public void Aspas_e_barra_invertida_nao_alteram_o_JQL(string versao, string esperado)
    {
        JiraRestClient.MontarJql("INV", versao).Should().Be(esperado);
    }

    [Fact]
    public void Projeto_tambem_e_escapado()
    {
        JiraRestClient.MontarJql("IN\"V", "1.0").Should().Be("project = \"IN\\\"V\" AND fixVersion = \"1.0\"");
    }

    [Fact]
    public async Task Release_Note_vem_do_campo_customizado_sem_chamada_extra()
    {
        var (cliente, handler) = Criar(campoCustomizado: "customfield_10050");
        handler.ResponderJson(Busca("""
            { "key": "INV-1", "fields": {
                "summary": "História", "description": "Técnico", "issuetype": { "name": "Story" }, "labels": [],
                "customfield_10050": { "type": "doc", "version": 1, "content": [
                  { "type": "paragraph", "content": [ { "type": "text", "text": "Texto para o cliente." } ] } ] },
                "subtasks": [ { "key": "INV-2", "fields": { "issuetype": { "name": "Release Note" } } } ] } }
            """));

        var historias = await cliente.BuscarHistoriasDaReleaseAsync("2026.10.0");

        historias.Single().TextoReleaseNote.Should().Be("Texto para o cliente.");
        handler.UrisChamadas.Should().ContainSingle("o campo já veio na busca; a subtarefa não é consultada");
        CamposEnviados(handler).Should().EndWith(",customfield_10050");
    }

    [Fact]
    public async Task Campo_customizado_vazio_cai_na_subtarefa()
    {
        var (cliente, handler) = Criar(campoCustomizado: "customfield_10050");
        handler
            .ResponderJson(Busca("""
                { "key": "INV-1", "fields": { "summary": "História", "description": "Técnico", "issuetype": { "name": "Story" },
                  "customfield_10050": null,
                  "subtasks": [ { "key": "INV-2", "fields": { "issuetype": { "name": "Release Note" } } } ] } }
                """))
            .ResponderJson("""{ "fields": { "description": "Texto da subtarefa." } }""");

        var historias = await cliente.BuscarHistoriasDaReleaseAsync("2026.10.0");

        historias.Single().TextoReleaseNote.Should().Be("Texto da subtarefa.");
        handler.UrisChamadas.Should().HaveCount(2);
    }

    [Fact]
    public async Task Sem_campo_customizado_le_a_subtarefa_do_tipo_configurado()
    {
        var (cliente, handler) = Criar(tipoSubtarefa: "Nota de Release");
        handler
            .ResponderJson(Busca("""
                { "key": "INV-1", "fields": { "summary": "História", "description": "Técnico", "issuetype": { "name": "Story" },
                  "subtasks": [
                    { "key": "INV-2", "fields": { "issuetype": { "name": "Release Note" } } },
                    { "key": "INV-3", "fields": { "issuetype": { "name": "nota de release" } } } ] } }
                """))
            .ResponderJson("""{ "fields": { "description": "Texto da nota." } }""");

        var historias = await cliente.BuscarHistoriasDaReleaseAsync("2026.10.0");

        historias.Single().TextoReleaseNote.Should().Be("Texto da nota.");
        handler.UrisChamadas[1]!.AbsolutePath.Should().Be("/rest/api/3/issue/INV-3");
        CamposEnviados(handler).Should().Be("summary,description,issuetype,labels,subtasks");
    }

    [Fact]
    public void Sem_projeto_o_Jira_nao_conta_como_configurado()
    {
        var opcoes = new JiraOptions { BaseUrl = "https://x.atlassian.net", Email = "a@b.com", ApiToken = "t" };

        opcoes.EstaConfigurado.Should().BeFalse();
        opcoes.Projeto = "INV";
        opcoes.EstaConfigurado.Should().BeTrue();
    }
}
