using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Database;
using InvoiSys.Tests.Fakes;
using InvoiSys.Tests.Unit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvoiSys.Tests.Integration;

[Collection("Postgres")]
public class HistoricoRevisaoPersistenciaTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private async Task<Release> CriarReleaseProcessada()
    {
        var release = new Release(
            $"RELEASE-HIST-{Guid.NewGuid():N}",
            [new HistoriaJira { Chave = "INV-1", Titulo = "Título", DescricaoTecnica = "Descrição", TipoIssue = "Story" }]);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Correcao, "Texto da IA", ["INV-1"])],
            "Título",
            "Resumo",
            AutenticacaoDeTeste.Revisor,
            T0);

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);
        return release;
    }

    private async Task Agir(Guid releaseId, Action<Release> acao)
    {
        await using var contexto = fixture.CriarContexto();
        var repositorio = new ReleaseRepository(contexto);
        var release = (await repositorio.BuscarPorIdAsync(releaseId))!;
        acao(release);
        await repositorio.SalvarAsync(release);
    }

    private async Task<IReadOnlyList<EventoRevisaoResumo>> Historico(Guid releaseId)
    {
        await using var contexto = fixture.CriarContexto();
        return await new HistoricoRevisaoRepository(contexto).ListarPorReleaseAsync(releaseId);
    }

    private async Task<VersaoComunicado> VersaoCliente(Guid releaseId)
    {
        await using var contexto = fixture.CriarContexto();
        return (await new ReleaseRepository(contexto).BuscarPorIdAsync(releaseId))!.VersaoCliente!;
    }

    [Fact]
    public async Task Aprovar_reabrir_aprovar_preserva_quem_aprovou_primeiro()
    {
        var ana = await fixture.SalvarRevisorAsync("Ana");
        var bia = await fixture.SalvarRevisorAsync("Bia");
        var release = await CriarReleaseProcessada();

        await Agir(release.Id, r => r.Aprovar(ana, T0.AddMinutes(1)));
        await Agir(release.Id, r => r.Reabrir(bia, T0.AddMinutes(2)));
        await Agir(release.Id, r => r.Aprovar(bia, T0.AddMinutes(3)));

        var historico = await Historico(release.Id);
        historico.Select(e => e.Acao).Should().Equal(
            AcaoRevisao.Processar, AcaoRevisao.Aprovar, AcaoRevisao.Reabrir, AcaoRevisao.Aprovar);
        historico.Select(e => e.UsuarioNome).Should().Equal("Victor Revisor", "Ana", "Bia", "Bia");
        historico.Select(e => e.UsuarioId).Should().Equal(
            AutenticacaoDeTeste.Revisor.Id, ana.Id, bia.Id, bia.Id);
        historico.Should().OnlyContain(e => e.Publico == PublicoAlvo.Cliente);
        historico.Select(e => e.OcorridoEm).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Retrato_atual_da_versao_bate_com_o_ultimo_evento_de_decisao()
    {
        var ana = await fixture.SalvarRevisorAsync("Ana");
        var bia = await fixture.SalvarRevisorAsync("Bia");
        var release = await CriarReleaseProcessada();
        await Agir(release.Id, r => r.Aprovar(ana, T0.AddMinutes(1)));
        await Agir(release.Id, r => r.Reabrir(bia, T0.AddMinutes(2)));
        await Agir(release.Id, r => r.Aprovar(bia, T0.AddMinutes(3)));

        var versao = await VersaoCliente(release.Id);
        var ultimaDecisao = (await Historico(release.Id)).Last(e => e.Acao is AcaoRevisao.Aprovar or AcaoRevisao.Reprovar);

        versao.RevisadoPorId.Should().Be(ultimaDecisao.UsuarioId);
        versao.RevisadoPor.Should().Be(ultimaDecisao.UsuarioNome);
        versao.RevisadoEm.Should().Be(ultimaDecisao.OcorridoEm);
    }

    [Fact]
    public async Task Eventos_pendentes_saem_da_fila_depois_de_salvar_e_nao_duplicam()
    {
        var release = await CriarReleaseProcessada();

        await using (var contexto = fixture.CriarContexto())
        {
            var repositorio = new ReleaseRepository(contexto);
            var carregada = (await repositorio.BuscarPorIdAsync(release.Id))!;
            carregada.Aprovar(AutenticacaoDeTeste.Revisor, T0.AddMinutes(1));
            carregada.EventosPendentes.Should().ContainSingle();

            await repositorio.SalvarAsync(carregada);
            carregada.EventosPendentes.Should().BeEmpty();
            await repositorio.SalvarAsync(carregada);
        }

        (await Historico(release.Id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Varios_eventos_na_mesma_transacao_e_no_mesmo_instante_mantem_a_ordem_pela_sequencia()
    {
        var release = await CriarReleaseProcessada();

        await Agir(release.Id, r =>
        {
            r.Aprovar(AutenticacaoDeTeste.Revisor, T0.AddMinutes(1));
            r.Reabrir(AutenticacaoDeTeste.Revisor, T0.AddMinutes(1));
            r.Aprovar(AutenticacaoDeTeste.Revisor, T0.AddMinutes(1));
        });

        (await Historico(release.Id)).Select(e => e.Acao).Should().Equal(
            AcaoRevisao.Processar, AcaoRevisao.Aprovar, AcaoRevisao.Reabrir, AcaoRevisao.Aprovar);
    }

    [Fact]
    public async Task Evento_e_agregado_gravam_na_mesma_transacao()
    {
        var release = await CriarReleaseProcessada();
        var naoPersistido = AutenticacaoDeTeste.UmRevisor("Fantasma");

        var acao = () => Agir(release.Id, r => r.Aprovar(naoPersistido, T0.AddMinutes(1)));

        await acao.Should().ThrowAsync<DbUpdateException>();
        (await VersaoCliente(release.Id)).Status.Should().Be(StatusRevisao.AguardandoRevisao);
        (await Historico(release.Id)).Should().ContainSingle(e => e.Acao == AcaoRevisao.Processar);
    }

    [Fact]
    public async Task Item_registra_quem_fez_e_o_detalhe_e_o_historico_sobrevive_ao_reprocessamento()
    {
        var ana = await fixture.SalvarRevisorAsync("Ana");
        var release = await CriarReleaseProcessada();
        var itemId = (await VersaoCliente(release.Id)).Itens[0].Id;

        await Agir(release.Id, r => r.EditarItem(itemId, "Texto revisado", ana, T0.AddMinutes(1)));
        await Agir(release.Id, r => r.ExcluirItem(itemId, ana, T0.AddMinutes(2), "Não interessa"));
        await Agir(release.Id, r => r.ReincluirItem(itemId, ana, T0.AddMinutes(3)));
        await Agir(release.Id, r => r.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto novo da IA", ["INV-1"])],
            "Título",
            "Resumo",
            ana,
            T0.AddMinutes(4)));

        var historico = await Historico(release.Id);
        historico.Select(e => e.Acao).Should().Equal(
            AcaoRevisao.Processar,
            AcaoRevisao.EditarItem,
            AcaoRevisao.ExcluirItem,
            AcaoRevisao.ReincluirItem,
            AcaoRevisao.Processar);
        historico[1].ItemId.Should().Be(itemId);
        historico[1].UsuarioNome.Should().Be("Ana");
        historico[1].Detalhe!["textoAnterior"].ToString().Should().Be("Texto da IA");
        historico[1].Detalhe!["textoNovo"].ToString().Should().Be("Texto revisado");
        historico[2].Detalhe!["motivo"].ToString().Should().Be("Não interessa");
        (await VersaoCliente(release.Id)).Itens.Should().NotContain(i => i.Id == itemId);
    }

    [Fact]
    public async Task Update_delete_e_truncate_em_eventos_revisao_sao_recusados_pelo_banco()
    {
        var release = await CriarReleaseProcessada();
        var evento = (await Historico(release.Id)).Single();
        await using var contexto = fixture.CriarContexto();

        var update = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE eventos_revisao SET acao = 'aprovar' WHERE id = {evento.Id}");
        var delete = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM eventos_revisao WHERE id = {evento.Id}");
        var truncate = () => contexto.Database.ExecuteSqlRawAsync("TRUNCATE eventos_revisao");

        (await update.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("só de inclusão");
        (await delete.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("só de inclusão");
        (await truncate.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("só de inclusão");
        (await Historico(release.Id)).Should().ContainSingle(e => e.Id == evento.Id && e.Acao == AcaoRevisao.Processar);
    }

    [Fact]
    public async Task CHECK_recusa_acao_fora_do_vocabulario()
    {
        var release = await CriarReleaseProcessada();
        var versao = await VersaoCliente(release.Id);
        await using var contexto = fixture.CriarContexto();

        var inserir = () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO eventos_revisao (id, release_id, versao_id, acao, usuario_id, ocorrido_em)
            VALUES ({Guid.NewGuid()}, {release.Id}, {versao.Id}, 'inventada', {AutenticacaoDeTeste.Revisor.Id}, now())
            """);

        (await inserir.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
    }

    [Fact]
    public async Task Chaves_estrangeiras_RESTRICT_impedem_apagar_release_ou_usuario_com_historico()
    {
        var ana = await fixture.SalvarRevisorAsync("Ana");
        var release = await CriarReleaseProcessada();
        await Agir(release.Id, r => r.Aprovar(ana, T0.AddMinutes(1)));
        await using var contexto = fixture.CriarContexto();

        var apagarRelease = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM releases WHERE id = {release.Id}");
        var apagarUsuario = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM usuarios WHERE id = {ana.Id}");

        await apagarRelease.Should().ThrowAsync<PostgresException>();
        await apagarUsuario.Should().ThrowAsync<PostgresException>();
    }
}
