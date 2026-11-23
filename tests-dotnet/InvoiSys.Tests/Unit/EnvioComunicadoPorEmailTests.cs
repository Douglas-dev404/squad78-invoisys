using FluentAssertions;
using InvoiSys.Application.Exportacao;
using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class EnvioComunicadoPorEmailTests
{
    private const string Chave = "RELEASE-2026-10";

    private readonly FakeReleaseRepository _releases = new();
    private readonly FakeComunicadoExportadoRepository _exportados = new();
    private readonly FakeDestinatarioRepository _destinatarios = new();
    private readonly FakeEnvioComunicadoRepository _envios = new();
    private readonly FakeUsuarioRepository _usuarios = new();
    private readonly FakeEnvioEmail _email = new();
    private readonly Usuario _revisor = AutenticacaoDeTeste.UmRevisor("Gestora");

    private readonly Destinatario _financeiro = new("Financeiro ACME", "financeiro@acme.com", PublicoAlvo.Cliente);
    private readonly Destinatario _ti = new("TI ACME", "ti@acme.com", PublicoAlvo.Cliente);

    public EnvioComunicadoPorEmailTests()
    {
        _usuarios.Usuarios.Add(_revisor);
        _destinatarios.Com(_financeiro, _ti);
    }

    private EnvioComunicadoPorEmail Criar() =>
        new(_releases, _exportados, _destinatarios, _envios, _usuarios, _email);

    private async Task<Release> ReleaseSalva(bool aprovada, string? titulo = "Novidades de outubro")
    {
        var release = new Release(Chave, []);
        release.ConcluirProcessamento(
            [new ItemComunicado(CategoriaAlteracao.Melhoria, "Busca mais rápida.", ["INV-7"])],
            titulo,
            "Resumo");
        if (aprovada)
        {
            release.Aprovar(_revisor, DateTimeOffset.UtcNow);
        }

        await _releases.SalvarAsync(release);
        return release;
    }

    private Task<ResultadoEnvio> Enviar(params Guid[] ids) =>
        Criar().EnviarAsync(Chave, PublicoAlvo.Cliente, ids, _revisor.Id);

    [Fact]
    public async Task Envia_html_e_texto_com_o_titulo_como_assunto_e_registra_exportacao_e_envios()
    {
        await ReleaseSalva(aprovada: true);

        var resultado = await Enviar(_ti.Id, _financeiro.Id);

        _email.Comunicados.Select(c => c.Email).Should().Equal("financeiro@acme.com", "ti@acme.com");
        _email.Comunicados.Should().OnlyContain(c => c.Assunto == "Novidades de outubro");
        _email.Comunicados[0].Html.Should().Contain("<!DOCTYPE html>").And.Contain("Busca mais r");
        _email.Comunicados[0].Texto.Should().Contain("Busca mais rápida.");

        _exportados.Exportados.Should().ContainSingle()
            .Which.Formato.Should().Be(FormatoExportacao.Html);
        resultado.Exportacao.GeradoPor.Should().Be("Gestora");
        _envios.Envios.Should().HaveCount(2).And.OnlyContain(
            e => e.Status == StatusEnvio.Enviado && e.ComunicadoExportadoId == resultado.Exportacao.Id);
    }

    [Fact]
    public async Task Sem_titulo_o_assunto_usa_a_chave_da_Release()
    {
        await ReleaseSalva(aprovada: true, titulo: null);

        await Enviar(_ti.Id);

        _email.Comunicados.Single().Assunto.Should().Be($"Release {Chave}");
    }

    [Fact]
    public async Task Versao_nao_aprovada_nao_envia_nem_grava_nada()
    {
        await ReleaseSalva(aprovada: false);

        var enviar = () => Enviar(_ti.Id);

        await enviar.Should().ThrowAsync<ReleaseNaoAprovadaException>();
        _email.Comunicados.Should().BeEmpty();
        _exportados.Exportados.Should().BeEmpty();
        _envios.Envios.Should().BeEmpty();
    }

    [Fact]
    public async Task Destinatario_de_outro_publico_ou_inativo_recusa_tudo_sem_enviar_a_ninguem()
    {
        await ReleaseSalva(aprovada: true);
        var interno = new Destinatario("Dev", "dev@invoisys.com", PublicoAlvo.Interno);
        var antigo = new Destinatario("Antigo", "antigo@acme.com", PublicoAlvo.Cliente);
        antigo.Desativar();
        _destinatarios.Com(interno, antigo);

        var outroPublico = () => Enviar(_ti.Id, interno.Id);
        var inativo = () => Enviar(_ti.Id, antigo.Id);
        var inexistente = () => Enviar(_ti.Id, Guid.NewGuid());

        (await outroPublico.Should().ThrowAsync<DestinatarioInvalidoParaEnvioException>())
            .Which.Message.Should().Contain("dev@invoisys.com").And.Contain("interno");
        (await inativo.Should().ThrowAsync<DestinatarioInvalidoParaEnvioException>())
            .Which.Message.Should().Contain("antigo@acme.com");
        await inexistente.Should().ThrowAsync<DestinatarioInvalidoParaEnvioException>();
        _email.Comunicados.Should().BeEmpty();
        _exportados.Exportados.Should().BeEmpty();
    }

    [Fact]
    public async Task Lista_vazia_ou_com_id_repetido_e_recusada()
    {
        await ReleaseSalva(aprovada: true);

        await Criar().Invoking(c => c.EnviarAsync(Chave, PublicoAlvo.Cliente, [], _revisor.Id))
            .Should().ThrowAsync<DestinatarioInvalidoParaEnvioException>();
        await Criar().Invoking(c => c.EnviarAsync(Chave, PublicoAlvo.Cliente, [_ti.Id, _ti.Id], _revisor.Id))
            .Should().ThrowAsync<DestinatarioInvalidoParaEnvioException>();
    }

    [Fact]
    public async Task Falha_de_um_destinatario_nao_impede_os_outros()
    {
        await ReleaseSalva(aprovada: true);
        _email.EmailsQueFalham.Add("ti@acme.com");

        var resultado = await Enviar(_financeiro.Id, _ti.Id);

        _email.Comunicados.Select(c => c.Email).Should().Equal("financeiro@acme.com");
        var falho = resultado.Envios.Single(e => e.Email == "ti@acme.com");
        falho.Status.Should().Be(StatusEnvio.Falhou);
        falho.Erro.Should().Be("Provedor de e-mail retornou 503.");
        resultado.Envios.Single(e => e.Email == "financeiro@acme.com").Status.Should().Be(StatusEnvio.Enviado);
        _envios.Envios.Should().HaveCount(2);
    }

    [Fact]
    public async Task Se_todos_falham_registra_as_falhas_e_lanca_erro_de_integracao()
    {
        await ReleaseSalva(aprovada: true);
        _email.EmailsQueFalham.Add("ti@acme.com");
        _email.EmailsQueFalham.Add("financeiro@acme.com");

        var enviar = () => Enviar(_financeiro.Id, _ti.Id);

        await enviar.Should().ThrowAsync<EnvioEmailException>();
        _envios.Envios.Should().HaveCount(2).And.OnlyContain(e => e.Status == StatusEnvio.Falhou);
    }

    [Fact]
    public async Task Release_inexistente_e_usuario_inativo_sao_recusados()
    {
        await Criar().Invoking(c => c.EnviarAsync("NAO-EXISTE", PublicoAlvo.Cliente, [_ti.Id], _revisor.Id))
            .Should().ThrowAsync<ReleaseNaoEncontradaException>();

        _revisor.Desativar();
        await ReleaseSalva(aprovada: false);
        await Criar().Invoking(c => c.EnviarAsync(Chave, PublicoAlvo.Cliente, [_ti.Id], _revisor.Id))
            .Should().ThrowAsync<RevisorInvalidoException>();
    }

    [Fact]
    public void Envio_nao_aceita_destinatario_de_publico_diferente_da_exportacao()
    {
        var release = new Release(Chave, []);
        release.ConcluirProcessamento([new ItemComunicado(CategoriaAlteracao.Melhoria, "x", ["INV-1"])], "T", "R");
        release.Aprovar(_revisor, DateTimeOffset.UtcNow);
        var exportado = new ComunicadoExportado(release.VersaoCliente!, FormatoExportacao.Html, "<p>x</p>", null, "G");

        var criar = () => EnvioComunicado.Enviado(
            exportado, new Destinatario("Dev", "dev@invoisys.com", PublicoAlvo.Interno));

        criar.Should().Throw<ArgumentException>();
    }
}
