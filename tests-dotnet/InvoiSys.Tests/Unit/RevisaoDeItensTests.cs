using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Tests.Unit;

/// <summary>
/// Revisão item a item pela Release (editar, excluir, reincluir). O ponto central: item
/// de versão aprovada não muda — o texto aprovado por um humano só é alterado depois de
/// reabrir a revisão (ADR-021).
/// </summary>
public class RevisaoDeItensTests
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static Release UmaReleaseProcessada()
    {
        var release = new Release(
            "RELEASE-2026-08",
            [new HistoriaJira { Chave = "INV-1", Titulo = "Título", DescricaoTecnica = "Descrição", TipoIssue = "Story" }]);

        release.ConcluirProcessamento(
            [
                new ItemComunicado(CategoriaAlteracao.Correcao, "Texto da IA", ["INV-1"]),
                new ItemComunicado(CategoriaAlteracao.Outros, "Item interno", ["INV-1"]),
            ],
            "Título",
            "Resumo");
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Texto para o suporte", ["INV-1"])],
            "Título S",
            "Resumo S",
            PublicoAlvo.Suporte);

        return release;
    }

    [Fact]
    public void Editar_item_troca_o_texto_final_e_preserva_o_texto_da_IA()
    {
        var release = UmaReleaseProcessada();
        var itemId = release.VersaoCliente!.Itens[0].Id;

        var item = release.EditarItem(itemId, "  Texto revisado por humano  ");

        item.TextoFinal.Should().Be("Texto revisado por humano");
        item.Texto.Should().Be("Texto da IA", "o que a IA gerou continua auditável");
    }

    [Fact]
    public void Editar_item_com_texto_vazio_e_recusado()
    {
        var release = UmaReleaseProcessada();
        var itemId = release.VersaoCliente!.Itens[0].Id;

        var acao = () => release.EditarItem(itemId, "   ");

        acao.Should().Throw<ArgumentException>();
        release.VersaoCliente!.Itens[0].TextoEditadoManualmente.Should().BeNull();
    }

    [Fact]
    public void Excluir_e_reincluir_acham_o_item_em_qualquer_versao()
    {
        var release = UmaReleaseProcessada();
        var suporte = release.VersaoPara(PublicoAlvo.Suporte)!;
        var itemId = suporte.Itens[0].Id;

        release.ExcluirItem(itemId, "  Não interessa ao suporte  ");

        suporte.ItensPublicaveis.Should().BeEmpty();
        suporte.Itens[0].MotivoExclusao.Should().Be("Não interessa ao suporte");
        release.VersaoCliente!.ItensPublicaveis.Should().HaveCount(2, "a versão de outro público não é tocada");

        release.ReincluirItem(itemId);

        suporte.ItensPublicaveis.Should().ContainSingle();
        suporte.Itens[0].MotivoExclusao.Should().BeNull();
    }

    [Fact]
    public void Excluir_sem_motivo_registra_motivo_nulo()
    {
        var release = UmaReleaseProcessada();
        var itemId = release.VersaoCliente!.Itens[1].Id;

        var item = release.ExcluirItem(itemId, "   ");

        item.Incluido.Should().BeFalse();
        item.MotivoExclusao.Should().BeNull();
    }

    [Fact]
    public void Item_inexistente_lanca_ItemNaoEncontradoException()
    {
        var release = UmaReleaseProcessada();

        var acao = () => release.EditarItem(Guid.NewGuid(), "Texto");

        acao.Should().Throw<ItemNaoEncontradoException>().WithMessage("*RELEASE-2026-08*");
    }

    [Fact]
    public void Item_de_versao_aprovada_nao_pode_ser_editado_excluido_nem_reincluido()
    {
        var release = UmaReleaseProcessada();
        var item = release.VersaoCliente!.Itens[0];
        release.Aprovar("revisora@invoisys.com", Agora);

        var editar = () => release.EditarItem(item.Id, "Texto depois da aprovação");
        var excluir = () => release.ExcluirItem(item.Id, "Depois da aprovação");
        var reincluir = () => release.ReincluirItem(item.Id);

        editar.Should().Throw<TransicaoDeStatusInvalidaException>().WithMessage("*reabra a revisão*");
        excluir.Should().Throw<TransicaoDeStatusInvalidaException>();
        reincluir.Should().Throw<TransicaoDeStatusInvalidaException>();

        item.TextoFinal.Should().Be("Texto da IA", "o conteúdo aprovado não mudou");
        item.Incluido.Should().BeTrue();
    }

    [Fact]
    public void Depois_de_reabrir_o_item_volta_a_poder_ser_editado()
    {
        var release = UmaReleaseProcessada();
        var itemId = release.VersaoCliente!.Itens[0].Id;
        release.Aprovar("revisora@invoisys.com", Agora);

        release.Reabrir();
        var item = release.EditarItem(itemId, "Correção depois de reabrir");

        item.TextoFinal.Should().Be("Correção depois de reabrir");
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
    }

    [Fact]
    public void Aprovar_um_publico_nao_trava_os_itens_de_outro()
    {
        var release = UmaReleaseProcessada();
        release.Aprovar("revisora@invoisys.com", Agora);
        var itemSuporte = release.VersaoPara(PublicoAlvo.Suporte)!.Itens[0];

        release.EditarItem(itemSuporte.Id, "Texto do suporte revisado");

        itemSuporte.TextoFinal.Should().Be("Texto do suporte revisado");
    }
}
