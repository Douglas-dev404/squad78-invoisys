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

public class ApiConsultaExportacoesTests
{
    private static WebApplicationFactory<Program> CriarApp(IComunicadoExportadoRepository exportados) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IComunicadoExportadoRepository>();
                services.ComUsuarios(AutenticacaoDeTeste.Revisor);
                services.AddScoped(_ => exportados);
            }));

    private static async Task<(FakeComunicadoExportadoRepository Repositorio, ComunicadoExportado Exportado)> UmaExportacao()
    {
        var release = new Release("RELEASE-2026-11", []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Item.", ["INV-1"])], "Título", "Resumo");
        release.Aprovar(AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);
        var exportado = new ComunicadoExportado(
            release.VersaoCliente!, FormatoExportacao.Markdown, "# Título", null, "Revisora");
        var repositorio = new FakeComunicadoExportadoRepository();
        await repositorio.AdicionarAsync(exportado);
        return (repositorio, exportado);
    }

    [Theory]
    [InlineData("/api/v1/exportacoes")]
    [InlineData("/api/v1/exportacoes/00000000-0000-0000-0000-000000000001")]
    public async Task Sem_token_responde_401(string rota)
    {
        using var app = CriarApp(new FakeComunicadoExportadoRepository());
        using var client = app.CreateClient();

        (await client.GetAsync(rota)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listar_sem_parametros_usa_pagina_1_tamanho_20_e_segue_o_contrato()
    {
        var (repositorio, exportado) = await UmaExportacao();
        using var app = CriarApp(repositorio);
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.GetAsync("/api/v1/exportacoes");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("total").GetInt32().Should().Be(1);
        corpo.GetProperty("pagina").GetInt32().Should().Be(1);
        corpo.GetProperty("tamanho").GetInt32().Should().Be(20);
        var item = corpo.GetProperty("itens")[0];
        item.GetProperty("id").GetGuid().Should().Be(exportado.Id);
        item.GetProperty("publico").GetString().Should().Be("cliente");
        item.GetProperty("formato").GetString().Should().Be("markdown");
        item.GetProperty("geradoPor").GetString().Should().Be("Revisora");
        item.TryGetProperty("chaveRelease", out _).Should().BeTrue();
        item.TryGetProperty("tituloExecutivo", out _).Should().BeTrue();
        item.TryGetProperty("geradoEm", out _).Should().BeTrue();
        item.TryGetProperty("conteudo", out _).Should().BeFalse("a listagem não traz o Markdown");
    }

    [Theory]
    [InlineData("tamanho=1000", "tamanho")]
    [InlineData("tamanho=0", "tamanho")]
    [InlineData("pagina=0", "pagina")]
    [InlineData("publico=xpto", "publico")]
    public async Task Parametro_fora_da_regra_responde_422_com_o_campo(string query, string campo)
    {
        using var app = CriarApp(new FakeComunicadoExportadoRepository());
        using var client = app.CriarClienteAutenticado();

        var resposta = await client.GetAsync($"/api/v1/exportacoes?{query}");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("errors").TryGetProperty(campo, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Detalhe_traz_o_conteudo_e_id_inexistente_responde_404()
    {
        var (repositorio, exportado) = await UmaExportacao();
        using var app = CriarApp(repositorio);
        using var client = app.CriarClienteAutenticado();

        var encontrado = await client.GetAsync($"/api/v1/exportacoes/{exportado.Id}");
        var inexistente = await client.GetAsync($"/api/v1/exportacoes/{Guid.NewGuid()}");

        encontrado.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await encontrado.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("conteudo").GetString().Should().Be("# Título");
        corpo.GetProperty("id").GetGuid().Should().Be(exportado.Id);
        inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
