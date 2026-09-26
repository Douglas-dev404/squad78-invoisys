using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Testes de integração de <see cref="ComunicadoExportadoRepository"/> contra Postgres
/// real — incluindo a FK <c>RESTRICT</c> que protege o histórico de auditoria, que só o
/// banco de verdade valida.
/// </summary>
[Collection("Postgres")]
public class ComunicadoExportadoRepositoryTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private static IEnumerable<ItemComunicado> UmItem() =>
        [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto", ["INV-1"])];

    /// <summary>Release com Cliente e Suporte aprovados, persistida pelo caminho de produção.</summary>
    private async Task<Release> SalvarReleaseAprovada()
    {
        var release = new Release($"RELEASE-TESTE-{Guid.NewGuid():N}", []);
        release.ConcluirProcessamento(UmItem(), "Título", "Resumo");
        release.ConcluirProcessamento(UmItem(), "Título S", "Resumo S", PublicoAlvo.Suporte);
        release.Aprovar("revisora@invoisys.com", Agora);
        release.Aprovar("revisora@invoisys.com", Agora, PublicoAlvo.Suporte);

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
}
