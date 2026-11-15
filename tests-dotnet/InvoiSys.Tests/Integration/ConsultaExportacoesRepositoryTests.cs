using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace InvoiSys.Tests.Integration;

// Container próprio: total e paginação contam a tabela inteira, e a coleção "Postgres" é compartilhada.
public class ConsultaExportacoesRepositoryTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly ITestOutputHelper _saida;

    public ConsultaExportacoesRepositoryTests(PostgresContainerFixture fixture, ITestOutputHelper saida)
    {
        _fixture = fixture;
        _saida = saida;
    }

    public async Task InitializeAsync()
    {
        await using var contexto = _fixture.CriarContexto();
        await contexto.Database.ExecuteSqlRawAsync(
            "TRUNCATE comunicados_exportados, releases RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<ComunicadoExportado>> ExportarAsync(
        string chave,
        string titulo,
        int vezes = 1,
        PublicoAlvo publico = PublicoAlvo.Cliente)
    {
        var revisor = await _fixture.SalvarRevisorAsync();
        var release = new Release(chave, [new HistoriaJira
        {
            Chave = "INV-1",
            Titulo = "Título",
            DescricaoTecnica = "Descrição técnica",
            TipoIssue = "Story",
        }]);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Item", ["INV-1"])], titulo, "Resumo", publico);
        release.Aprovar(revisor, DateTimeOffset.UtcNow, publico);

        await using var contexto = _fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);
        var repositorio = new ComunicadoExportadoRepository(contexto);
        var criados = new List<ComunicadoExportado>();
        for (var i = 0; i < vezes; i++)
        {
            var exportado = new ComunicadoExportado(
                release.VersaoPara(publico)!, FormatoExportacao.Markdown, $"# {titulo} ({i})", null, "Revisora");
            await repositorio.AdicionarAsync(exportado);
            criados.Add(exportado);
        }

        return criados;
    }

    private async Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico = null,
        string? busca = null,
        int pagina = 1,
        int tamanho = 20)
    {
        await using var contexto = _fixture.CriarContexto(_saida);
        return await new ComunicadoExportadoRepository(contexto).ListarAsync(publico, busca, pagina, tamanho);
    }

    [Fact]
    public async Task Banco_vazio_devolve_pagina_vazia_e_total_zero()
    {
        var pagina = await ListarAsync();

        pagina.Total.Should().Be(0);
        pagina.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Duas_paginas_tem_o_mesmo_total_sem_repetir_item_e_mais_recente_primeiro()
    {
        var criados = await ExportarAsync("2026.11.0", "Novidades de novembro", vezes: 3);

        var primeira = await ListarAsync(tamanho: 2);
        var segunda = await ListarAsync(pagina: 2, tamanho: 2);

        primeira.Total.Should().Be(3);
        segunda.Total.Should().Be(3);
        primeira.Itens.Should().HaveCount(2);
        segunda.Itens.Should().HaveCount(1);
        primeira.Itens.Concat(segunda.Itens).Select(i => i.Id)
            .Should().Equal(criados.OrderByDescending(c => c.GeradoEm).ThenByDescending(c => c.Id).Select(c => c.Id));
        primeira.Itens.Should().OnlyContain(i => i.Conteudo == null, "a listagem não carrega o Markdown");
    }

    [Fact]
    public async Task Item_traz_chave_da_release_e_titulo_da_versao()
    {
        await ExportarAsync("2026.11.0", "Novidades de novembro");

        var item = (await ListarAsync()).Itens.Single();

        item.ChaveRelease.Should().Be("2026.11.0");
        item.TituloExecutivo.Should().Be("Novidades de novembro");
        item.Publico.Should().Be(PublicoAlvo.Cliente);
        item.Formato.Should().Be(FormatoExportacao.Markdown);
        item.GeradoPor.Should().Be("Revisora");
    }

    [Fact]
    public async Task Filtro_de_publico_so_devolve_aquele_publico()
    {
        await ExportarAsync("2026.11.0", "Para o cliente");
        await ExportarAsync("2026.11.1", "Para o comercial", publico: PublicoAlvo.Comercial);

        var comercial = await ListarAsync(PublicoAlvo.Comercial);

        comercial.Total.Should().Be(1);
        comercial.Itens.Single().ChaveRelease.Should().Be("2026.11.1");
    }

    [Theory]
    [InlineData("11.1", "2026.11.1")]
    [InlineData("DEZEMBRO", "2026.12.0")]
    [InlineData("correções", "2026.11.1")]
    public async Task Busca_casa_parte_da_chave_ou_do_titulo_sem_diferenciar_maiusculas(string busca, string esperada)
    {
        await ExportarAsync("2026.11.0", "Novidades de novembro");
        await ExportarAsync("2026.11.1", "Correções da folha");
        await ExportarAsync("2026.12.0", "Novidades de dezembro");

        var resultado = await ListarAsync(busca: busca);

        resultado.Itens.Select(i => i.ChaveRelease).Should().Equal(esperada);
        resultado.Total.Should().Be(1);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    public async Task Curinga_do_like_digitado_na_busca_e_tratado_como_texto(string busca)
    {
        await ExportarAsync("2026.11.0", "Desconto de 10% na fatura");
        await ExportarAsync("2026.11.1", "Sem caracteres especiais");

        var resultado = await ListarAsync(busca: busca);

        resultado.Itens.Select(i => i.ChaveRelease)
            .Should().Equal(busca == "%" ? ["2026.11.0"] : Array.Empty<string>());
    }

    [Fact]
    public async Task Detalhe_traz_o_conteudo_e_id_inexistente_devolve_nulo()
    {
        var criado = (await ExportarAsync("2026.11.0", "Novidades de novembro")).Single();

        await using var contexto = _fixture.CriarContexto();
        var repositorio = new ComunicadoExportadoRepository(contexto);
        var detalhe = await repositorio.BuscarDetalheAsync(criado.Id);
        var inexistente = await repositorio.BuscarDetalheAsync(Guid.NewGuid());

        detalhe.Should().NotBeNull();
        detalhe!.Conteudo.Should().Be("# Novidades de novembro (0)");
        detalhe.ChaveRelease.Should().Be("2026.11.0");
        inexistente.Should().BeNull();
    }
}
