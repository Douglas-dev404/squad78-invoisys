using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// Os invariantes de negócio do agregado Release. Estes testes são o contrato: a
/// revisão humana é obrigatória, e não existe caminho de código que pule esse gate.
/// </summary>
public class ReleaseInvariantesTests
{
    private static HistoriaJira UmaHistoria(string chave = "INV-1") => new()
    {
        Chave = chave,
        Titulo = "Título",
        DescricaoTecnica = "Descrição técnica",
        TipoIssue = "Story",
    };

    private static ItemComunicado UmItem() =>
        new(CategoriaAlteracao.Melhoria, "Texto de negócio", ["INV-1"]);

    [Fact]
    public void Release_nasce_pendente_e_nao_pronta_para_exportar()
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);

        release.Status.Should().Be(StatusPipeline.Pendente);
        release.ProntaParaExportar.Should().BeFalse();
        release.Itens.Should().BeEmpty();
    }

    [Fact]
    public void Concluir_processamento_deixa_a_release_aguardando_revisao_nunca_aprovada()
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);
        release.MarcarProcessando();

        release.ConcluirProcessamento([UmItem()], "Título executivo", "Resumo executivo");

        release.Status.Should().Be(StatusPipeline.AguardandoRevisao);
        release.TituloExecutivo.Should().Be("Título executivo");
        release.ResumoExecutivo.Should().Be("Resumo executivo");
        release.ProntaParaExportar.Should().BeFalse();
    }

    [Fact]
    public void Aprovar_sem_itens_processados_falha()
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);

        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        acao.Should().Throw<ReleaseSemItensProcessadosException>();
        release.Status.Should().Be(StatusPipeline.Pendente);
    }

    [Fact]
    public void Aprovar_pulando_a_fila_de_pendente_falha()
    {
        // Cenário: alguém injeta itens e tenta aprovar sem passar por
        // AguardandoRevisao. Como só ConcluirProcessamento popula os itens, o caminho
        // realista é tentar aprovar duas vezes — a segunda já não está aguardando.
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);
        release.ConcluirProcessamento([UmItem()], "Título", "Resumo");
        release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        var acao = () => release.Aprovar("outro.revisor", DateTimeOffset.UtcNow);

        acao.Should().Throw<RevisaoHumanaObrigatoriaException>();
        release.AprovadoPor.Should().Be("darth.code");
    }

    [Fact]
    public void Aprovar_apos_revisao_registra_quem_e_quando_e_libera_exportacao()
    {
        var agora = DateTimeOffset.UtcNow;
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);
        release.ConcluirProcessamento([UmItem()], "Título", "Resumo");

        release.Aprovar("darth.code", agora);

        release.Status.Should().Be(StatusPipeline.Aprovado);
        release.AprovadoPor.Should().Be("darth.code");
        release.AprovadoEm.Should().Be(agora);
        release.ProntaParaExportar.Should().BeTrue();
    }

    [Fact]
    public void Release_que_falhou_nao_pode_ser_aprovada()
    {
        var release = new Release("RELEASE-2026-08", [UmaHistoria()]);
        release.ConcluirProcessamento([UmItem()], "Título", "Resumo");
        release.MarcarFalha();

        var acao = () => release.Aprovar("darth.code", DateTimeOffset.UtcNow);

        acao.Should().Throw<RevisaoHumanaObrigatoriaException>();
    }

    [Fact]
    public void Edicao_humana_sobrescreve_o_texto_da_IA_nunca_o_contrario()
    {
        var item = UmItem();
        item.TextoFinal.Should().Be("Texto de negócio");

        item.EditarManualmente("Texto revisado por humano");

        item.TextoFinal.Should().Be("Texto revisado por humano");
        item.Texto.Should().Be("Texto de negócio", "o texto original da IA é preservado");
    }
}
