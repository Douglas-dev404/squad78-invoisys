using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiSys.Tests.Integration;

public class ApiExportacaoTests
{
    private const string Chave = "RELEASE-2026-08";

    private static WebApplicationFactory<Program> CriarApp(
        IReleaseRepository releases,
        IComunicadoExportadoRepository exportados) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReleaseRepository>();
                services.RemoveAll<IComunicadoExportadoRepository>();
                services.AddScoped(_ => releases);
                services.AddScoped(_ => exportados);
            }));

    private static async Task<(FakeReleaseRepository Releases, Release Release)> ReleaseSalva(
        bool aprovada)
    {
        var release = new Release(Chave, []);
        release.ConcluirProcessamento(
            [
                new ItemComunicado(CategoriaAlteracao.NovaFuncionalidade, "Item publicado.", ["INV-1"]),
                new ItemComunicado(CategoriaAlteracao.Correcao, "Item excluído.", ["INV-2"]),
            ],
            "Título da Release",
            "Resumo da Release");

        release.ExcluirItem(release.Itens[1].Id, "interno");

        if (aprovada)
        {
            release.Aprovar("victor", DateTimeOffset.UtcNow);
        }

        var releases = new FakeReleaseRepository();
        await releases.SalvarAsync(release);
        return (releases, release);
    }

    private static async Task<HttpResponseMessage> Exportar(
        IReleaseRepository releases,
        IComunicadoExportadoRepository exportados,
        object corpo,
        string chave = Chave)
    {
        using var app = CriarApp(releases, exportados);
        using var client = app.CreateClient();
        return await client.PostAsJsonAsync($"/api/v1/releases/{chave}/exportar", corpo);
    }

    [Fact]
    public async Task Exportar_versao_aprovada_responde_201_e_persiste_o_markdown()
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);
        var exportados = new FakeComunicadoExportadoRepository();

        var resposta = await Exportar(
            releases,
            exportados,
            new { publico = "cliente", formato = "markdown", geradoPor = "victor" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("formato").GetString().Should().Be("markdown");
        corpo.GetProperty("publico").GetString().Should().Be("cliente");
        corpo.GetProperty("conteudo").GetString().Should().Contain("## Novas Funcionalidades");

        var salvo = exportados.Exportados.Should().ContainSingle().Subject;
        salvo.Conteudo.Should().Contain("Item publicado.");
        salvo.GeradoPor.Should().Be("victor");
    }

    [Fact]
    public async Task Exportar_nao_inclui_item_excluido()
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);
        var exportados = new FakeComunicadoExportadoRepository();

        var resposta = await Exportar(releases, exportados, new { publico = "cliente", formato = "markdown" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var conteudo = exportados.Exportados.Single().Conteudo;
        conteudo.Should().Contain("Item publicado.");
        conteudo.Should().NotContain("Item excluído.");
        conteudo.Should().NotContain("## Correções");
    }

    [Fact]
    public async Task Exportar_sem_gerado_por_registra_sistema()
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);
        var exportados = new FakeComunicadoExportadoRepository();

        await Exportar(releases, exportados, new { publico = "cliente", formato = "markdown" });

        exportados.Exportados.Single().GeradoPor.Should().Be("sistema");
    }

    [Fact]
    public async Task Exportar_sem_aprovacao_responde_409_em_ProblemDetails_e_nao_persiste()
    {
        var (releases, _) = await ReleaseSalva(aprovada: false);
        var exportados = new FakeComunicadoExportadoRepository();

        var resposta = await Exportar(releases, exportados, new { publico = "cliente", formato = "markdown" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("não está aprovada");
        exportados.Exportados.Should().BeEmpty();
    }

    [Fact]
    public async Task Exportar_publico_sem_comunicado_responde_409()
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);

        var resposta = await Exportar(
            releases,
            new FakeComunicadoExportadoRepository(),
            new { publico = "suporte", formato = "markdown" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Exportar_release_inexistente_responde_404()
    {
        var resposta = await Exportar(
            new FakeReleaseRepository(),
            new FakeComunicadoExportadoRepository(),
            new { publico = "cliente", formato = "markdown" },
            chave: "RELEASE-INEXISTENTE");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("financeiro", "markdown")]
    [InlineData(null, "markdown")]
    [InlineData("cliente", "docx")]
    [InlineData("cliente", null)]
    [InlineData("cliente", "html")]
    [InlineData("cliente", "pdf")]
    public async Task Exportar_com_publico_ou_formato_invalido_ou_nao_suportado_responde_422(
        string? publico,
        string? formato)
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);
        var exportados = new FakeComunicadoExportadoRepository();

        var resposta = await Exportar(releases, exportados, new { publico, formato });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        exportados.Exportados.Should().BeEmpty();
    }

    [Fact]
    public async Task Reexportar_gera_nova_linha_no_historico()
    {
        var (releases, _) = await ReleaseSalva(aprovada: true);
        var exportados = new FakeComunicadoExportadoRepository();
        var corpo = new { publico = "cliente", formato = "markdown" };

        await Exportar(releases, exportados, corpo);
        await Exportar(releases, exportados, corpo);

        exportados.Exportados.Should().HaveCount(2);
        exportados.Exportados.Select(e => e.Id).Distinct().Should().HaveCount(2);
    }
}
