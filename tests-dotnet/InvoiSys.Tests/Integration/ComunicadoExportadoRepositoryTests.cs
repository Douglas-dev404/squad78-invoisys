using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class ComunicadoExportadoRepositoryTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private static IEnumerable<ItemComunicado> UmItem() =>
        [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto", ["INV-1"])];

    private async Task<Release> SalvarReleaseAprovada()
    {
        var release = new Release($"RELEASE-TESTE-{Guid.NewGuid():N}", []);
        release.ConcluirProcessamento(UmItem(), "Título", "Resumo");
        release.ConcluirProcessamento(UmItem(), "Título S", "Resumo S", PublicoAlvo.Suporte);
        var revisor = await fixture.SalvarRevisorAsync();
        release.Aprovar(revisor, Agora);
        release.Aprovar(revisor, Agora, PublicoAlvo.Suporte);

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);

        return release;
    }

    private async Task<ComunicadoExportado> Exportar(VersaoComunicado versao, FormatoExportacao formato)
    {
        var exportado = new ComunicadoExportado(versao, formato, "# Conteúdo", null, "revisora@invoisys.com");

        await using var contexto = fixture.CriarContexto();
        await new ComunicadoExportadoRepository(contexto).AdicionarAsync(exportado);

        return exportado;
    }

    [Fact]
    public async Task AdicionarAsync_persiste_e_BuscarPorIdAsync_traz_todos_os_campos()
    {
        var release = await SalvarReleaseAprovada();
        var exportado = await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);

        await using var contexto = fixture.CriarContexto();
        var encontrado = await new ComunicadoExportadoRepository(contexto).BuscarPorIdAsync(exportado.Id);

        encontrado.Should().NotBeNull();
        encontrado!.ReleaseId.Should().Be(release.Id);
        encontrado.VersaoComunicadoId.Should().Be(release.VersaoCliente!.Id);
        encontrado.Publico.Should().Be(PublicoAlvo.Cliente);
        encontrado.Formato.Should().Be(FormatoExportacao.Markdown);
        encontrado.Conteudo.Should().Be("# Conteúdo");
        encontrado.GeradoPor.Should().Be("revisora@invoisys.com");
    }

    [Fact]
    public async Task ListarPorReleaseAsync_guarda_cada_reexportacao_e_filtra_por_publico()
    {
        var release = await SalvarReleaseAprovada();
        await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);
        await Task.Delay(5);
        await Exportar(release.VersaoCliente!, FormatoExportacao.Html);
        await Task.Delay(5);
        await Exportar(release.VersaoPara(PublicoAlvo.Suporte)!, FormatoExportacao.Markdown);

        await using var contexto = fixture.CriarContexto();
        var repositorio = new ComunicadoExportadoRepository(contexto);

        var todos = await repositorio.ListarPorReleaseAsync(release.Id);
        var doCliente = await repositorio.ListarPorReleaseAsync(release.Id, PublicoAlvo.Cliente);

        todos.Should().HaveCount(3);
        todos[0].Publico.Should().Be(PublicoAlvo.Suporte, "a mais recente vem primeiro");
        doCliente.Select(c => c.Formato)
            .Should().Equal(FormatoExportacao.Html, FormatoExportacao.Markdown);
        (await repositorio.ListarPorReleaseAsync(Guid.NewGuid())).Should().BeEmpty();
    }

    [Fact]
    public async Task Apagar_versao_que_ja_foi_exportada_e_barrado_pelo_banco()
    {
        var release = await SalvarReleaseAprovada();
        await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);

        await using var contexto = fixture.CriarContexto();
        var versao = await contexto.VersoesComunicado.SingleAsync(v => v.Id == release.VersaoCliente!.Id);
        contexto.VersoesComunicado.Remove(versao);

        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>(
            "FK RESTRICT: o histórico de publicação não se reescreve por efeito colateral");
    }

    private static string PrefixoUnico() => $"H{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    private async Task<Release> SalvarReleaseAprovada(string chave, string tituloCliente, string tituloSuporte)
    {
        var release = new Release(chave, []);
        release.ConcluirProcessamento(UmItem(), tituloCliente, "Resumo");
        release.ConcluirProcessamento(UmItem(), tituloSuporte, "Resumo S", PublicoAlvo.Suporte);
        var revisor = await fixture.SalvarRevisorAsync();
        release.Aprovar(revisor, Agora);
        release.Aprovar(revisor, Agora, PublicoAlvo.Suporte);

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);

        return release;
    }

    private async Task<PaginaExportacoes> Listar(
        PublicoAlvo? publico = null,
        string? busca = null,
        int pagina = 1,
        int tamanho = 20)
    {
        await using var contexto = fixture.CriarContexto();
        return await new ComunicadoExportadoRepository(contexto)
            .ListarAsync(publico, busca, pagina, tamanho);
    }

    [Fact]
    public async Task ListarAsync_sem_exportacoes_que_casem_devolve_pagina_vazia_e_total_zero()
    {
        var resultado = await Listar(busca: PrefixoUnico());

        resultado.Itens.Should().BeEmpty();
        resultado.Total.Should().Be(0);
    }

    [Fact]
    public async Task ListarAsync_pagina_no_banco_sem_mudar_o_total_e_do_mais_recente_ao_mais_antigo()
    {
        var prefixo = PrefixoUnico();
        var release = await SalvarReleaseAprovada($"{prefixo}-1", "Título cliente", "Título suporte");
        var primeiro = await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);
        await Task.Delay(5);
        var segundo = await Exportar(release.VersaoPara(PublicoAlvo.Suporte)!, FormatoExportacao.Markdown);
        await Task.Delay(5);
        var terceiro = await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);

        var paginaUm = await Listar(busca: prefixo, pagina: 1, tamanho: 2);
        var paginaDois = await Listar(busca: prefixo, pagina: 2, tamanho: 2);

        paginaUm.Total.Should().Be(3);
        paginaDois.Total.Should().Be(3, "o total não depende da página");
        paginaUm.Itens.Select(i => i.Id).Should().Equal(terceiro.Id, segundo.Id);
        paginaDois.Itens.Select(i => i.Id).Should().Equal(primeiro.Id);
    }

    [Fact]
    public async Task ListarAsync_filtra_por_publico()
    {
        var prefixo = PrefixoUnico();
        var release = await SalvarReleaseAprovada($"{prefixo}-1", "Título cliente", "Título suporte");
        await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);
        var doSuporte = await Exportar(release.VersaoPara(PublicoAlvo.Suporte)!, FormatoExportacao.Markdown);

        var resultado = await Listar(publico: PublicoAlvo.Suporte, busca: prefixo);

        resultado.Total.Should().Be(1);
        resultado.Itens.Should().ContainSingle().Which.Id.Should().Be(doSuporte.Id);
        resultado.Itens[0].TituloExecutivo.Should().Be("Título suporte", "o título vem da versão exportada");
    }

    [Fact]
    public async Task ListarAsync_busca_parte_da_chave_ou_do_titulo_sem_diferenciar_maiusculas()
    {
        var prefixo = PrefixoUnico();
        var nfe = await SalvarReleaseAprovada($"{prefixo}-INV-123", "Emissão de NF-e mais rápida", "S");
        var icms = await SalvarReleaseAprovada($"{prefixo}-FIS-9", $"Correção no ICMS {prefixo}", "S");
        var exportadoNfe = await Exportar(nfe.VersaoCliente!, FormatoExportacao.Markdown);
        var exportadoIcms = await Exportar(icms.VersaoCliente!, FormatoExportacao.Markdown);

        var porChave = await Listar(busca: $"{prefixo.ToLowerInvariant()}-inv-1");
        var porTitulo = await Listar(busca: $"icms {prefixo.ToLowerInvariant()}");

        porChave.Itens.Should().ContainSingle().Which.Id.Should().Be(exportadoNfe.Id);
        porChave.Itens[0].ChaveRelease.Should().Be($"{prefixo}-INV-123");
        porTitulo.Itens.Should().ContainSingle().Which.Id.Should().Be(exportadoIcms.Id);
    }

    [Fact]
    public async Task ListarAsync_trata_porcentagem_e_sublinhado_da_busca_como_texto()
    {
        var prefixo = PrefixoUnico();
        var comCuringas = await SalvarReleaseAprovada($"{prefixo}_A", $"{prefixo} desconto de 10% no frete", "S");
        var semCuringas = await SalvarReleaseAprovada($"{prefixo}XA", $"{prefixo} desconto de 10 reais no frete", "S");
        var exportadoComCuringas = await Exportar(comCuringas.VersaoCliente!, FormatoExportacao.Markdown);
        await Exportar(semCuringas.VersaoCliente!, FormatoExportacao.Markdown);

        var porPorcentagem = await Listar(busca: $"{prefixo} desconto de 10%");
        var porSublinhado = await Listar(busca: $"{prefixo}_A");

        porPorcentagem.Itens.Should().ContainSingle().Which.Id.Should().Be(exportadoComCuringas.Id);
        porSublinhado.Itens.Should().ContainSingle().Which.Id.Should().Be(exportadoComCuringas.Id);
    }

    [Fact]
    public async Task BuscarDetalheAsync_traz_conteudo_chave_e_titulo()
    {
        var release = await SalvarReleaseAprovada();
        var exportado = await Exportar(release.VersaoCliente!, FormatoExportacao.Markdown);

        await using var contexto = fixture.CriarContexto();
        var detalhe = await new ComunicadoExportadoRepository(contexto).BuscarDetalheAsync(exportado.Id);

        detalhe.Should().NotBeNull();
        detalhe!.ChaveRelease.Should().Be(release.ChaveJira);
        detalhe.TituloExecutivo.Should().Be("Título");
        detalhe.Publico.Should().Be(PublicoAlvo.Cliente);
        detalhe.Formato.Should().Be(FormatoExportacao.Markdown);
        detalhe.Conteudo.Should().Be("# Conteúdo");
        detalhe.GeradoPor.Should().Be("revisora@invoisys.com");
    }

    [Fact]
    public async Task BuscarDetalheAsync_de_id_inexistente_devolve_null()
    {
        await using var contexto = fixture.CriarContexto();

        (await new ComunicadoExportadoRepository(contexto).BuscarDetalheAsync(Guid.NewGuid()))
            .Should().BeNull();
    }
}
