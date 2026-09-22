using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// Invariantes da revisão por público. O ponto central: cada
/// <see cref="VersaoComunicado"/> é revisada de forma independente — aprovar a versão
/// do Cliente não libera a exportação do Suporte, e vice-versa.
/// </summary>
public class VersaoComunicadoTests
{
    private static HistoriaJira UmaHistoria(string chave = "INV-1") => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static ItemComunicado UmItem(string texto = "Texto de negócio") =>
        new(CategoriaAlteracao.Melhoria, texto, ["INV-1"]);

    private static Release UmaReleaseProcessada(params PublicoAlvo[] publicos)
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);

        foreach (var publico in publicos.DefaultIfEmpty(PublicoAlvo.Cliente))
        {
            release.ConcluirProcessamento(
                [UmItem($"Texto para {publico.ParaValor()}")],
                $"Título {publico.ParaValor()}",
                $"Resumo {publico.ParaValor()}",
                publico);
        }

        return release;
    }

    [Fact]
    public void Versao_nasce_aguardando_revisao_e_nao_pronta_para_exportar()
    {
        var release = UmaReleaseProcessada();
        var versao = release.VersaoCliente!;

        versao.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        versao.ProntaParaExportar.Should().BeFalse();
    }

    [Fact]
    public void Cada_publico_tem_titulo_e_resumo_proprios()
    {
        var release = UmaReleaseProcessada(PublicoAlvo.Cliente, PublicoAlvo.Suporte);

        release.VersaoPara(PublicoAlvo.Cliente)!.TituloExecutivo.Should().Be("Título cliente");
        release.VersaoPara(PublicoAlvo.Suporte)!.TituloExecutivo.Should().Be("Título suporte");
    }

    [Fact]
    public void Aprovar_um_publico_nao_libera_a_exportacao_dos_outros()
    {
        var release = UmaReleaseProcessada(PublicoAlvo.Cliente, PublicoAlvo.Suporte);

        release.Aprovar("darth.code", DateTimeOffset.UtcNow, PublicoAlvo.Cliente);

        release.VersaoPara(PublicoAlvo.Cliente)!.ProntaParaExportar.Should().BeTrue();
        release.VersaoPara(PublicoAlvo.Suporte)!.ProntaParaExportar.Should().BeFalse();
    }

    [Fact]
    public void Release_so_fica_aprovada_quando_todos_os_publicos_foram_aprovados()
    {
        var agora = DateTimeOffset.UtcNow;
        var release = UmaReleaseProcessada(PublicoAlvo.Cliente, PublicoAlvo.Suporte);

        release.Aprovar("darth.code", agora, PublicoAlvo.Cliente);
        release.Status.Should().Be(
            StatusPipeline.AguardandoRevisao,
            "o Suporte ainda não foi revisado");

        release.Aprovar("darth.code", agora, PublicoAlvo.Suporte);
        release.Status.Should().Be(StatusPipeline.Aprovado);
    }

    [Fact]
    public void Exportar_versao_nao_aprovada_e_impossivel_ate_em_memoria()
    {
        var release = UmaReleaseProcessada();

        var acao = () => new ComunicadoExportado(
            release.VersaoCliente!,
            FormatoExportacao.Markdown,
            "# Comunicado",
            null,
            "sistema");

        acao.Should().Throw<ReleaseNaoAprovadaException>();
    }

    [Fact]
    public void Exportar_versao_aprovada_registra_publico_e_versao_de_origem()
    {
        var release = UmaReleaseProcessada(PublicoAlvo.Comercial);
        release.Aprovar("darth.code", DateTimeOffset.UtcNow, PublicoAlvo.Comercial);
        var versao = release.VersaoPara(PublicoAlvo.Comercial)!;

        var exportado = new ComunicadoExportado(
            versao,
            FormatoExportacao.Markdown,
            "# Comunicado",
            null,
            "sistema");

        exportado.Publico.Should().Be(PublicoAlvo.Comercial);
        exportado.VersaoComunicadoId.Should().Be(versao.Id);
        exportado.ReleaseId.Should().Be(release.Id);
    }

    [Fact]
    public void Reprovar_exige_motivo_e_devolve_a_versao_para_a_fila()
    {
        var release = UmaReleaseProcessada();

        release.Reprovar("darth.code", "Texto genérico demais", DateTimeOffset.UtcNow);

        var versao = release.VersaoCliente!;
        versao.Status.Should().Be(StatusRevisao.Reprovado);
        versao.MotivoReprovacao.Should().Be("Texto genérico demais");
        versao.ProntaParaExportar.Should().BeFalse();
    }

    [Fact]
    public void Reprovar_sem_motivo_falha()
    {
        var release = UmaReleaseProcessada();

        var acao = () => release.Reprovar("darth.code", "   ", DateTimeOffset.UtcNow);

        acao.Should().Throw<ArgumentException>();
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
    }

    [Fact]
    public void Versao_reprovada_nao_pode_ser_aprovada_sem_reprocessar()
    {
        var release = UmaReleaseProcessada();
        release.Reprovar("darth.code", "Precisa refazer", DateTimeOffset.UtcNow);

        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        acao.Should().Throw<RevisaoHumanaObrigatoriaException>();
    }

    [Fact]
    public void Reabrir_versao_aprovada_bloqueia_a_exportacao_de_novo()
    {
        var release = UmaReleaseProcessada();
        release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        release.Reabrir();

        var versao = release.VersaoCliente!;
        versao.Status.Should().Be(StatusRevisao.AguardandoRevisao);
        versao.RevisadoPor.Should().BeNull();
        versao.ProntaParaExportar.Should().BeFalse();
        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);
    }

    [Fact]
    public void Reabrir_versao_que_nunca_foi_aprovada_falha()
    {
        var release = UmaReleaseProcessada();

        var acao = () => release.Reabrir();

        acao.Should().Throw<TransicaoDeStatusInvalidaException>();
    }

    [Fact]
    public void Reprocessar_um_publico_nao_toca_nos_outros()
    {
        var agora = DateTimeOffset.UtcNow;
        var release = UmaReleaseProcessada(PublicoAlvo.Cliente, PublicoAlvo.Suporte);
        release.Aprovar("darth.code", agora, PublicoAlvo.Cliente);

        release.ConcluirProcessamento(
            [UmItem("Texto refeito")],
            "Novo título",
            "Novo resumo",
            PublicoAlvo.Suporte);

        release.VersaoPara(PublicoAlvo.Cliente)!.Status.Should().Be(StatusRevisao.Aprovado);
        release.VersaoPara(PublicoAlvo.Suporte)!.TituloExecutivo.Should().Be("Novo título");
        release.Versoes.Should().HaveCount(2, "reprocessar substitui a versão, não duplica");
    }

    [Fact]
    public void Item_excluido_sai_do_comunicado_mas_continua_auditavel()
    {
        var release = UmaReleaseProcessada();
        var item = release.VersaoCliente!.Itens[0];

        item.Excluir("Alteração interna, não interessa ao cliente");

        release.VersaoCliente!.ItensPublicaveis.Should().BeEmpty();
        release.VersaoCliente!.Itens.Should().HaveCount(1, "o registro do que a IA gerou é preservado");
        item.MotivoExclusao.Should().Be("Alteração interna, não interessa ao cliente");
    }

    [Fact]
    public void Aprovar_versao_com_todos_os_itens_excluidos_falha()
    {
        var release = UmaReleaseProcessada();
        release.VersaoCliente!.Itens[0].Excluir("Interno");

        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        acao.Should().Throw<ReleaseSemItensProcessadosException>();
    }

    [Fact]
    public void Item_reincluido_volta_para_o_comunicado()
    {
        var release = UmaReleaseProcessada();
        var item = release.VersaoCliente!.Itens[0];
        item.Excluir("Engano");

        item.Reincluir();

        release.VersaoCliente!.ItensPublicaveis.Should().HaveCount(1);
        item.MotivoExclusao.Should().BeNull();
    }

    [Fact]
    public void Pipeline_registra_execucao_no_agregado_da_release()
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);

        var execucao = release.RegistrarExecucao("openai/gpt-4o-mini");

        release.Execucoes.Should().ContainSingle().Which.Should().BeSameAs(execucao);
        execucao.Status.Should().Be(StatusExecucaoPipeline.Iniciada);
        execucao.ReleaseId.Should().Be(release.Id);
    }
}
