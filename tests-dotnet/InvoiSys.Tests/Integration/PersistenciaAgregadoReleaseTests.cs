using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Tests.Integration;

/// <summary>
/// Ciclo de vida completo do agregado <see cref="Release"/> contra Postgres real, do
/// jeito que a aplicação vai usá-lo: cada etapa num <see cref="InvoiSysDbContext"/> novo
/// (um por request), carregando a Release já persistida, mutando pelos métodos de
/// domínio e salvando de volta.
///
/// O ponto sensível é o segundo save em diante: versões, itens e execuções nascem com
/// <c>Guid.NewGuid()</c> no construtor e entram numa Release que já está rastreada. Se o
/// EF tratar a chave preenchida como "entidade existente", o save vira UPDATE de linha
/// que não existe — por isso cada etapa relê tudo num contexto limpo.
/// </summary>
[Collection("Postgres")]
public class PersistenciaAgregadoReleaseTests(PostgresContainerFixture fixture)
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private static HistoriaJira UmaHistoria(string chave) => new()
    {
        Chave = chave,
        Titulo = $"Título {chave}",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static List<ItemComunicado> ItensGerados(params string[] textos) =>
        [.. textos.Select((texto, i) => new ItemComunicado(
            CategoriaAlteracao.Melhoria, texto, [$"INV-{i + 1}"]))];

    private async Task<Release> CriarReleasePersistida()
    {
        var release = new Release(
            $"RELEASE-TESTE-{Guid.NewGuid():N}",
            [UmaHistoria("INV-1"), UmaHistoria("INV-2")]);

        await using var contexto = fixture.CriarContexto();
        await new ReleaseRepository(contexto).SalvarAsync(release);

        return release;
    }

    private async Task Etapa(Guid releaseId, Action<Release> mutacao)
    {
        await using var contexto = fixture.CriarContexto();
        var repositorio = new ReleaseRepository(contexto);

        var release = await repositorio.BuscarPorIdAsync(releaseId);
        mutacao(release!);
        await repositorio.SalvarAsync(release!);
    }

    private async Task<Release> Recarregar(Guid releaseId)
    {
        await using var contexto = fixture.CriarContexto();
        return (await new ReleaseRepository(contexto).BuscarPorIdAsync(releaseId))!;
    }

    [Fact]
    public async Task Processamento_em_release_ja_persistida_grava_versoes_itens_e_execucao()
    {
        var release = await CriarReleasePersistida();

        await Etapa(release.Id, r =>
        {
            r.MarcarProcessando();
            var execucao = r.RegistrarExecucao("openai/gpt-4o-mini");
            r.ConcluirProcessamento(ItensGerados("Novo filtro", "Correção no XML"), "Título", "Resumo");
            r.ConcluirProcessamento(ItensGerados("Texto técnico"), "Título S", "Resumo S", PublicoAlvo.Suporte);
            execucao.MarcarConcluida();
        });

        var recarregada = await Recarregar(release.Id);

        recarregada.Status.Should().Be(StatusPipeline.AguardandoRevisao);
        recarregada.Versoes.Should().HaveCount(2);
        recarregada.VersaoCliente!.TituloExecutivo.Should().Be("Título");
        recarregada.VersaoCliente.Itens.Select(i => i.Texto)
            .Should().BeEquivalentTo("Novo filtro", "Correção no XML");
        recarregada.VersaoPara(PublicoAlvo.Suporte)!.Itens.Should().ContainSingle();

        var execucaoGravada = recarregada.Execucoes.Should().ContainSingle().Subject;
        execucaoGravada.Status.Should().Be(StatusExecucaoPipeline.Concluida);
        execucaoGravada.ModeloLlm.Should().Be("openai/gpt-4o-mini");
        execucaoGravada.ConcluidoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Revisao_humana_persiste_edicao_exclusao_e_aprovacao_por_publico()
    {
        var release = await CriarReleasePersistida();
        await Etapa(release.Id, r =>
        {
            r.ConcluirProcessamento(ItensGerados("Texto da IA", "Item interno"), "Título", "Resumo");
            r.ConcluirProcessamento(ItensGerados("Texto técnico"), "Título S", "Resumo S", PublicoAlvo.Suporte);
        });

        await Etapa(release.Id, r =>
        {
            var itens = r.VersaoCliente!.Itens;
            itens.Single(i => i.Texto == "Texto da IA").EditarManualmente("Texto revisado");
            itens.Single(i => i.Texto == "Item interno").Excluir("Não interessa ao cliente");
            r.Aprovar("revisora@invoisys.com", Agora);
        });

        var recarregada = await Recarregar(release.Id);
        var cliente = recarregada.VersaoCliente!;

        cliente.Status.Should().Be(StatusRevisao.Aprovado);
        cliente.RevisadoPor.Should().Be("revisora@invoisys.com");
        cliente.RevisadoEm.Should().Be(Agora);
        cliente.ItensPublicaveis.Should().ContainSingle()
            .Which.TextoFinal.Should().Be("Texto revisado");

        // Edição humana não sobrescreve o que a IA gerou; exclusão não apaga o registro.
        cliente.Itens.Single(i => i.Texto == "Texto da IA").TextoEditadoManualmente
            .Should().Be("Texto revisado");
        var excluido = cliente.Itens.Single(i => i.Texto == "Item interno");
        excluido.Incluido.Should().BeFalse();
        excluido.MotivoExclusao.Should().Be("Não interessa ao cliente");

        // Aprovar o Cliente não libera o Suporte, e a Release só fica Aprovada com todas.
        recarregada.VersaoPara(PublicoAlvo.Suporte)!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        recarregada.Status.Should().Be(StatusPipeline.AguardandoRevisao);
    }

    [Fact]
    public async Task Reprovacao_persiste_motivo()
    {
        var release = await CriarReleasePersistida();
        await Etapa(release.Id, r => r.ConcluirProcessamento(ItensGerados("Texto"), "Título", "Resumo"));

        await Etapa(release.Id, r => r.Reprovar("revisora@invoisys.com", "Tom técnico demais", Agora));

        var cliente = (await Recarregar(release.Id)).VersaoCliente!;
        cliente.Status.Should().Be(StatusRevisao.Reprovado);
        cliente.MotivoReprovacao.Should().Be("Tom técnico demais");
    }

    [Fact]
    public async Task Reprocessar_um_publico_substitui_os_itens_sem_deixar_orfaos_e_mantem_o_historico_de_execucoes()
    {
        var release = await CriarReleasePersistida();
        await Etapa(release.Id, r =>
        {
            r.RegistrarExecucao("modelo-a").MarcarConcluida();
            r.ConcluirProcessamento(ItensGerados("Versão 1a", "Versão 1b"), "Título 1", "Resumo 1");
        });
        var idVersaoOriginal = (await Recarregar(release.Id)).VersaoCliente!.Id;

        await Etapa(release.Id, r =>
        {
            r.RegistrarExecucao("modelo-b").MarcarConcluida();
            r.ConcluirProcessamento(ItensGerados("Versão 2"), "Título 2", "Resumo 2");
        });

        var recarregada = await Recarregar(release.Id);
        var cliente = recarregada.VersaoCliente!;

        cliente.Id.Should().Be(idVersaoOriginal, "reprocessar reaproveita a versão do público");
        cliente.TituloExecutivo.Should().Be("Título 2");
        cliente.Itens.Select(i => i.Texto).Should().Equal("Versão 2");
        recarregada.Execucoes.Select(e => e.ModeloLlm).Should().BeEquivalentTo("modelo-a", "modelo-b");

        await using var contexto = fixture.CriarContexto();
        var itensNoBanco = await contexto.Set<ItemComunicado>()
            .CountAsync(i => EF.Property<Guid>(i, "VersaoComunicadoId") == idVersaoOriginal);
        itensNoBanco.Should().Be(1, "os itens da geração anterior não podem ficar órfãos na tabela");
    }

    [Fact]
    public async Task Execucao_com_falha_sobrevive_ao_save()
    {
        var release = await CriarReleasePersistida();

        await Etapa(release.Id, r =>
        {
            r.MarcarProcessando();
            r.RegistrarExecucao("openai/gpt-4o-mini").MarcarFalha("OpenRouter devolveu 502");
            r.MarcarFalha();
        });

        var recarregada = await Recarregar(release.Id);

        recarregada.Status.Should().Be(StatusPipeline.Falhou);
        var execucao = recarregada.Execucoes.Should().ContainSingle().Subject;
        execucao.Status.Should().Be(StatusExecucaoPipeline.Falhou);
        execucao.Erro.Should().Be("OpenRouter devolveu 502");
    }
}
