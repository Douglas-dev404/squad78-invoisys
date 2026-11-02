using System.Reflection;
using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using static InvoiSys.Tests.Fakes.AutenticacaoDeTeste;

namespace InvoiSys.Tests.Unit;

public class EventosRevisaoTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static Release UmaReleaseProcessada(Usuario? responsavel = null)
    {
        var release = new Release(
            "RELEASE-2026-11",
            [new HistoriaJira { Chave = "INV-1", Titulo = "Título", DescricaoTecnica = "Descrição", TipoIssue = "Story" }]);

        release.ConcluirProcessamento(
            [
                new ItemComunicado(CategoriaAlteracao.Correcao, "Texto da IA", ["INV-1"]),
                new ItemComunicado(CategoriaAlteracao.Outros, "Item interno", ["INV-1"]),
            ],
            "Título",
            "Resumo",
            responsavel ?? Revisor,
            T0);
        return release;
    }

    [Fact]
    public void Processar_gera_um_evento_com_usuario_horario_e_total_de_itens()
    {
        var ana = UmRevisor("Ana");

        var release = UmaReleaseProcessada(ana);

        var evento = release.EventosPendentes.Should().ContainSingle().Subject;
        evento.Acao.Should().Be(AcaoRevisao.Processar);
        evento.UsuarioId.Should().Be(ana.Id);
        evento.OcorridoEm.Should().Be(T0);
        evento.ReleaseId.Should().Be(release.Id);
        evento.VersaoId.Should().Be(release.VersaoCliente!.Id);
        evento.ItemId.Should().BeNull();
        evento.Detalhe!["totalItens"].Should().Be(2);
    }

    [Fact]
    public void Aprovar_reprovar_e_reabrir_geram_exatamente_um_evento_cada()
    {
        var release = UmaReleaseProcessada();
        release.LimparEventosPendentes();
        var ana = UmRevisor("Ana");

        release.Aprovar(ana, T0.AddMinutes(1));
        release.Reabrir(ana, T0.AddMinutes(2));
        release.Reprovar(ana, "Tom técnico demais", T0.AddMinutes(3));

        release.EventosPendentes.Select(e => e.Acao).Should().Equal(
            AcaoRevisao.Aprovar, AcaoRevisao.Reabrir, AcaoRevisao.Reprovar);
        release.EventosPendentes[2].Detalhe!["motivo"].Should().Be("Tom técnico demais");
        release.EventosPendentes.Select(e => e.UsuarioId).Should().OnlyContain(id => id == ana.Id);
        release.EventosPendentes.Select(e => e.OcorridoEm).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Aprovar_reabrir_aprovar_preserva_quem_aprovou_primeiro()
    {
        var release = UmaReleaseProcessada();
        release.LimparEventosPendentes();
        var ana = UmRevisor("Ana");
        var bia = UmRevisor("Bia");

        release.Aprovar(ana, T0.AddMinutes(1));
        release.Reabrir(bia, T0.AddMinutes(2));
        release.Aprovar(bia, T0.AddMinutes(3));

        release.EventosPendentes.Select(e => (e.Acao, e.UsuarioId)).Should().Equal(
            (AcaoRevisao.Aprovar, ana.Id),
            (AcaoRevisao.Reabrir, bia.Id),
            (AcaoRevisao.Aprovar, bia.Id));
        release.VersaoCliente!.RevisadoPorId.Should().Be(bia.Id, "o retrato atual é a última decisão");
    }

    [Fact]
    public void Item_editado_excluido_e_reincluido_registra_quem_fez_e_o_detalhe()
    {
        var release = UmaReleaseProcessada();
        release.LimparEventosPendentes();
        var item = release.VersaoCliente!.Itens[0];
        var ana = UmRevisor("Ana");

        release.EditarItem(item.Id, "Texto revisado", ana, T0.AddMinutes(1));
        release.ExcluirItem(item.Id, ana, T0.AddMinutes(2), "Não interessa ao cliente");
        release.ReincluirItem(item.Id, ana, T0.AddMinutes(3));

        release.EventosPendentes.Select(e => e.Acao).Should().Equal(
            AcaoRevisao.EditarItem, AcaoRevisao.ExcluirItem, AcaoRevisao.ReincluirItem);
        release.EventosPendentes.Should().OnlyContain(e => e.ItemId == item.Id && e.UsuarioId == ana.Id);
        release.EventosPendentes[0].Detalhe!["textoAnterior"].Should().Be("Texto da IA");
        release.EventosPendentes[0].Detalhe!["textoNovo"].Should().Be("Texto revisado");
        release.EventosPendentes[1].Detalhe!["motivo"].Should().Be("Não interessa ao cliente");
        release.EventosPendentes[1].Detalhe!["texto"].Should().Be("Texto revisado");
        release.EventosPendentes[2].Detalhe!["texto"].Should().Be("Texto revisado");
    }

    [Fact]
    public void Acao_recusada_pela_regra_de_negocio_nao_gera_evento()
    {
        var release = UmaReleaseProcessada();
        release.LimparEventosPendentes();
        var item = release.VersaoCliente!.Itens[0];
        var ana = UmRevisor("Ana");
        release.Aprovar(ana, T0);
        release.LimparEventosPendentes();

        var editar = () => release.EditarItem(item.Id, "Novo texto", ana, T0);
        var aprovarDeNovo = () => release.Aprovar(ana, T0);
        var reabrirSemAprovada = () => UmaReleaseProcessada().Reabrir(ana, T0);

        editar.Should().Throw<TransicaoDeStatusInvalidaException>();
        aprovarDeNovo.Should().Throw<RevisaoHumanaObrigatoriaException>();
        reabrirSemAprovada.Should().Throw<TransicaoDeStatusInvalidaException>();
        release.EventosPendentes.Should().BeEmpty();
    }

    [Fact]
    public void Usuario_nulo_ou_desativado_nao_executa_nenhuma_acao()
    {
        var release = UmaReleaseProcessada();
        release.LimparEventosPendentes();
        var item = release.VersaoCliente!.Itens[0];
        var inativo = UmRevisor("Inativo");
        inativo.Desativar();

        var acoes = new List<Action>
        {
            () => release.Aprovar(inativo, T0),
            () => release.Reprovar(inativo, "motivo", T0),
            () => release.Reabrir(inativo, T0),
            () => release.EditarItem(item.Id, "x", inativo, T0),
            () => release.ExcluirItem(item.Id, inativo, T0),
            () => release.ReincluirItem(item.Id, inativo, T0),
            () => release.ConcluirProcessamento([], "T", "R", inativo, T0),
            () => release.Aprovar(null!, T0),
            () => release.EditarItem(item.Id, "x", null!, T0),
        };

        foreach (var acao in acoes)
        {
            acao.Should().Throw<ArgumentException>();
        }

        release.EventosPendentes.Should().BeEmpty();
        release.VersaoCliente!.Status.Should().Be(StatusRevisao.AguardandoRevisao);
    }

    [Fact]
    public void LimparEventosPendentes_esvazia_a_fila()
    {
        var release = UmaReleaseProcessada();

        release.LimparEventosPendentes();

        release.EventosPendentes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(VersaoComunicado))]
    [InlineData(typeof(ItemComunicado))]
    [InlineData(typeof(EventoRevisao))]
    public void Entidades_filhas_nao_expoem_metodo_publico_que_mude_estado(Type tipo)
    {
        var publicos = tipo
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name);

        publicos.Should().BeEmpty(
            "só a Release muda estado de versão, item e evento: é o que garante um evento por ação");
    }

    [Fact]
    public void EventoRevisao_nao_tem_setter_publico_nem_construtor_publico()
    {
        var tipo = typeof(EventoRevisao);

        tipo.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
        tipo.GetProperties().Select(p => p.SetMethod)
            .Where(s => s is { IsPublic: true } && !s.ReturnParameter.GetRequiredCustomModifiers()
                .Any(m => m.Name == "IsExternalInit"))
            .Should().BeEmpty();
    }
}
