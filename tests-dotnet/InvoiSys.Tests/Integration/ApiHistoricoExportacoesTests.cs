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

public class ApiHistoricoExportacoesTests
{
    private const string Chave = "INV-123";

    private static WebApplicationFactory<Program> CriarApp(IComunicadoExportadoRepository exportados) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IComunicadoExportadoRepository>();
                services.ComUsuarios(AutenticacaoDeTeste.Revisor);
                services.AddScoped(_ => exportados);
            }));

    private static async Task<(FakeComunicadoExportadoRepository Exportados, ComunicadoExportado Exportado)>
        UmaExportacao()
    {
        var release = new Release(Chave, []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Item.", ["INV-1"])],
            "Título executivo",
            "Resumo");
        release.Aprovar(AutenticacaoDeTeste.Revisor, DateTimeOffset.UtcNow);

        var exportados = new FakeComunicadoExportadoRepository().ComRelease(release);
        var exportado = new ComunicadoExportado(
            release.VersaoCliente!,
            FormatoExportacao.Markdown,
            "# Comunicado",
            null,
            "Maria Souza");
        await exportados.AdicionarAsync(exportado);

        return (exportados, exportado);
    }

    private static async Task<HttpResponseMessage> Get(IComunicadoExportadoRepository exportados, string url)
    {
        using var app = CriarApp(exportados);
        using var client = app.CriarClienteAutenticado();
        return await client.GetAsync(url);
    }

    [Fact]
    public async Task Listar_sem_token_responde_401()
    {
        using var app = CriarApp(new FakeComunicadoExportadoRepository());
        using var client = app.CreateClient();

        var resposta = await client.GetAsync("/api/v1/exportacoes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listar_responde_200_no_contrato_da_issue_com_paginacao_padrao()
    {
        var (exportados, exportado) = await UmaExportacao();

        var resposta = await Get(exportados, "/api/v1/exportacoes");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("itens", "total", "pagina", "tamanho");
        corpo.GetProperty("total").GetInt32().Should().Be(1);
        corpo.GetProperty("pagina").GetInt32().Should().Be(1);
        corpo.GetProperty("tamanho").GetInt32().Should().Be(20);

        var item = corpo.GetProperty("itens")[0];
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "chaveRelease", "tituloExecutivo", "publico", "formato", "geradoPor", "geradoEm");
        item.GetProperty("id").GetGuid().Should().Be(exportado.Id);
        item.GetProperty("chaveRelease").GetString().Should().Be(Chave);
        item.GetProperty("tituloExecutivo").GetString().Should().Be("Título executivo");
        item.GetProperty("publico").GetString().Should().Be("cliente");
        item.GetProperty("formato").GetString().Should().Be("markdown");
        item.GetProperty("geradoPor").GetString().Should().Be("Maria Souza");
    }

    [Fact]
    public async Task Listar_repassa_filtros_e_devolve_pagina_e_tamanho_pedidos()
    {
        var (exportados, _) = await UmaExportacao();

        var resposta = await Get(exportados, "/api/v1/exportacoes?pagina=2&tamanho=5&publico=suporte&busca=inv");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("itens").GetArrayLength().Should().Be(0);
        corpo.GetProperty("total").GetInt32().Should().Be(0);
        corpo.GetProperty("pagina").GetInt32().Should().Be(2);
        corpo.GetProperty("tamanho").GetInt32().Should().Be(5);
    }

    [Theory]
    [InlineData("tamanho=1000", "tamanho")]
    [InlineData("tamanho=0", "tamanho")]
    [InlineData("pagina=0", "pagina")]
    [InlineData("publico=diretoria", "publico")]
    public async Task Listar_com_parametro_fora_da_faixa_responde_422(string consulta, string campo)
    {
        var resposta = await Get(new FakeComunicadoExportadoRepository(), $"/api/v1/exportacoes?{consulta}");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("errors").TryGetProperty(campo, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Detalhe_responde_200_com_conteudo()
    {
        var (exportados, exportado) = await UmaExportacao();

        var resposta = await Get(exportados, $"/api/v1/exportacoes/{exportado.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("id").GetGuid().Should().Be(exportado.Id);
        corpo.GetProperty("chaveRelease").GetString().Should().Be(Chave);
        corpo.GetProperty("conteudo").GetString().Should().Be("# Comunicado");
    }

    [Fact]
    public async Task Detalhe_de_id_inexistente_responde_404_em_ProblemDetails()
    {
        var resposta = await Get(new FakeComunicadoExportadoRepository(), $"/api/v1/exportacoes/{Guid.NewGuid()}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
