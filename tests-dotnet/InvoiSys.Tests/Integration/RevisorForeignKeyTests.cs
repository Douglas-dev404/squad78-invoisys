using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class RevisorForeignKeyTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    private static Release UmaReleaseProcessada()
    {
        var release = new Release($"RELEASE-FK-{Guid.NewGuid():N}", []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto", ["INV-1"])],
            "Título",
            "Resumo");
        return release;
    }

    private async Task Salvar(Release release)
    {
        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);
    }

    private async Task<VersaoComunicado> Recarregar(Guid releaseId)
    {
        await using var contexto = fixture.CriarContexto();
        var release = await new ReleaseRepository(contexto).BuscarPorIdAsync(releaseId);
        return release!.VersaoCliente!;
    }

    [Fact]
    public async Task Aprovar_persiste_o_id_do_revisor_e_o_nome_exibido()
    {
        var revisor = await fixture.SalvarRevisorAsync("Ana Revisora");
        var release = UmaReleaseProcessada();
        release.Aprovar(revisor, Agora);

        await Salvar(release);

        var versao = await Recarregar(release.Id);
        versao.RevisadoPorId.Should().Be(revisor.Id);
        versao.RevisadoPor.Should().Be("Ana Revisora");
    }

    [Fact]
    public async Task Reabrir_zera_o_id_do_revisor_no_banco()
    {
        var revisor = await fixture.SalvarRevisorAsync();
        var release = UmaReleaseProcessada();
        release.Aprovar(revisor, Agora);
        await Salvar(release);

        await using (var contexto = fixture.CriarContexto())
        {
            var repositorio = new ReleaseRepository(contexto);
            var carregada = (await repositorio.BuscarPorIdAsync(release.Id))!;
            carregada.Reabrir();
            await repositorio.SalvarAsync(carregada);
        }

        (await Recarregar(release.Id)).RevisadoPorId.Should().BeNull();
    }

    [Fact]
    public async Task FK_recusa_revisor_que_nao_existe_em_usuarios()
    {
        var release = UmaReleaseProcessada();
        release.Aprovar(AutenticacaoDeTesteUsuarioNaoPersistido(), Agora);

        var acao = () => Salvar(release);

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RESTRICT_impede_apagar_usuario_que_ja_revisou()
    {
        var revisor = await fixture.SalvarRevisorAsync();
        var release = UmaReleaseProcessada();
        release.Aprovar(revisor, Agora);
        await Salvar(release);

        await using var contexto = fixture.CriarContexto();
        var acao = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM usuarios WHERE id = {revisor.Id}");

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Exportacao_persiste_gerado_por_id_e_a_FK_protege_o_usuario()
    {
        var revisor = await fixture.SalvarRevisorAsync();
        var release = UmaReleaseProcessada();
        release.Aprovar(revisor, Agora);
        await Salvar(release);
        var exportado = new ComunicadoExportado(
            release.VersaoCliente!,
            FormatoExportacao.Markdown,
            "# Comunicado",
            null,
            revisor.Nome,
            revisor.Id);

        await using (var contexto = fixture.CriarContexto())
        {
            await new ComunicadoExportadoRepository(contexto).AdicionarAsync(exportado);
        }

        await using var leitura = fixture.CriarContexto();
        var lido = await leitura.ComunicadosExportados.AsNoTracking().SingleAsync(c => c.Id == exportado.Id);
        lido.GeradoPorId.Should().Be(revisor.Id);
        lido.GeradoPor.Should().Be(revisor.Nome);

        var apagar = () => leitura.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM usuarios WHERE id = {revisor.Id}");
        await apagar.Should().ThrowAsync<DbUpdateException>();
    }

    private static Usuario AutenticacaoDeTesteUsuarioNaoPersistido() =>
        Fakes.AutenticacaoDeTeste.UmRevisor("Fantasma");
}
