using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace InvoiSys.Tests.Integration;

// Container próprio: o dashboard conta o banco inteiro, e a coleção "Postgres" é compartilhada.
public class DashboardRepositoryTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly ITestOutputHelper _saida;

    public DashboardRepositoryTests(PostgresContainerFixture fixture, ITestOutputHelper saida)
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

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    private static HistoriaJira Historia(string chave = "INV-1") => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static Release ReleaseGerada(string titulo = "Título executivo", int itens = 1)
    {
        var release = new Release($"REL-{Guid.NewGuid():N}", [Historia()]);
        release.ConcluirProcessamento(
            Enumerable.Range(1, itens)
                .Select(i => new ItemComunicado(CategoriaAlteracao.Melhoria, $"Item {i}", ["INV-1"])),
            titulo,
            "Resumo");
        return release;
    }

    private async Task SalvarAsync(params Release[] releases)
    {
        await using var contexto = _fixture.CriarContexto();
        var repositorio = new ReleaseRepository(contexto);
        foreach (var release in releases)
        {
            await repositorio.SalvarAsync(release);
        }
    }

    private async Task<DashboardDados> BuscarAsync()
    {
        await using var contexto = _fixture.CriarContexto(_saida);
        return await new ReleaseRepository(contexto).BuscarDashboardAsync(Hoje);
    }

    [Fact]
    public async Task Banco_vazio_devolve_tudo_zerado_e_sete_dias_sem_erro()
    {
        var dados = await BuscarAsync();

        dados.Aprovadas.Should().Be(0);
        dados.AguardandoRevisao.Should().Be(0);
        dados.Falharam.Should().Be(0);
        dados.Exportacoes.Should().Be(0);
        dados.ItensGerados.Should().Be(0);
        dados.ItensEditados.Should().Be(0);
        dados.ItensExcluidos.Should().Be(0);
        dados.Recentes.Should().BeEmpty();
        dados.UltimosDias.Should().HaveCount(7);
        dados.UltimosDias.Select(d => d.Data).Should().Equal(
            Enumerable.Range(0, 7).Select(i => Hoje.AddDays(i - 6)));
        dados.UltimosDias.Should().OnlyContain(d => d.Aprovadas == 0 && d.Geradas == 0);
    }

    [Fact]
    public async Task Conta_uma_release_de_cada_status()
    {
        var revisor = await _fixture.SalvarRevisorAsync();
        var aprovada = ReleaseGerada();
        aprovada.Aprovar(revisor, DateTimeOffset.UtcNow);
        var aguardando = ReleaseGerada();
        var falhou = new Release($"REL-{Guid.NewGuid():N}", [Historia()]);
        falhou.MarcarProcessando();
        falhou.MarcarFalha();
        var pendente = new Release($"REL-{Guid.NewGuid():N}", [Historia()]);
        await SalvarAsync(aprovada, aguardando, falhou, pendente);

        var dados = await BuscarAsync();

        dados.Aprovadas.Should().Be(1);
        dados.AguardandoRevisao.Should().Be(1);
        dados.Falharam.Should().Be(1);
        dados.Recentes.Should().HaveCount(4);
    }

    [Fact]
    public async Task Qualidade_conta_itens_gerados_editados_e_excluidos()
    {
        var release = ReleaseGerada(itens: 3);
        var itens = release.Itens.ToList();
        release.EditarItem(itens[0].Id, "Texto revisado pelo humano");
        release.ExcluirItem(itens[1].Id, "Interno");
        await SalvarAsync(release);

        var dados = await BuscarAsync();

        dados.ItensGerados.Should().Be(3);
        dados.ItensEditados.Should().Be(1);
        dados.ItensExcluidos.Should().Be(1);
    }

    [Fact]
    public async Task Ultimos_sete_dias_poe_geradas_e_aprovadas_no_dia_certo_e_zera_os_demais()
    {
        var revisor = await _fixture.SalvarRevisorAsync();
        var aprovadaHaTresDias = ReleaseGerada();
        aprovadaHaTresDias.Aprovar(revisor, new DateTimeOffset(Hoje.AddDays(-3).ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero));
        var aprovadaHaDezDias = ReleaseGerada();
        aprovadaHaDezDias.Aprovar(revisor, new DateTimeOffset(Hoje.AddDays(-10).ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero));
        await SalvarAsync(aprovadaHaTresDias, aprovadaHaDezDias, ReleaseGerada());

        var dados = await BuscarAsync();

        dados.UltimosDias.Should().HaveCount(7);
        dados.UltimosDias.Single(d => d.Data == Hoje).Geradas.Should().Be(3);
        dados.UltimosDias.Single(d => d.Data == Hoje.AddDays(-3)).Aprovadas.Should().Be(1);
        dados.UltimosDias.Sum(d => d.Aprovadas).Should().Be(1);
        dados.UltimosDias.Where(d => d.Data != Hoje).Should().OnlyContain(d => d.Geradas == 0);
    }

    [Fact]
    public async Task Recentes_traz_o_titulo_da_versao_cliente_mais_novas_primeiro_e_no_maximo_dez()
    {
        for (var i = 0; i < 12; i++)
        {
            await SalvarAsync(ReleaseGerada($"Título {i}"));
        }

        var dados = await BuscarAsync();

        dados.Recentes.Should().HaveCount(10);
        dados.Recentes.Should().BeInDescendingOrder(r => r.CriadoEm);
        dados.Recentes[0].TituloExecutivo.Should().Be("Título 11");
        dados.Recentes[0].Status.Should().Be(StatusPipeline.AguardandoRevisao);
    }
}
