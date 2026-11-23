using FluentAssertions;
using InvoiSys.Application.Destinatarios;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class CadastroDestinatariosTests
{
    [Fact]
    public void Destinatario_normaliza_email_tira_espacos_do_nome_e_nasce_ativo()
    {
        var destinatario = new Destinatario("  Financeiro ACME ", " Financeiro@ACME.com.br ", PublicoAlvo.Cliente);

        destinatario.Nome.Should().Be("Financeiro ACME");
        destinatario.Email.Should().Be("financeiro@acme.com.br");
        destinatario.Publico.Should().Be(PublicoAlvo.Cliente);
        destinatario.Ativo.Should().BeTrue();
        destinatario.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("  ", "a@b.com")]
    [InlineData("Nome", "")]
    public void Destinatario_sem_nome_ou_email_e_recusado(string nome, string email)
    {
        var criar = () => new Destinatario(nome, email, PublicoAlvo.Cliente);

        criar.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Desativar_e_reativar_mudam_so_a_situacao()
    {
        var destinatario = new Destinatario("TI", "ti@acme.com", PublicoAlvo.Suporte);

        destinatario.Desativar();
        destinatario.Ativo.Should().BeFalse();

        destinatario.Reativar();
        destinatario.Ativo.Should().BeTrue();
        destinatario.Email.Should().Be("ti@acme.com");
    }

    [Fact]
    public async Task Cadastrar_persiste_o_destinatario()
    {
        var repositorio = new FakeDestinatarioRepository();

        var destinatario = await new CadastroDestinatarios(repositorio)
            .CadastrarAsync("Comercial", "vendas@acme.com", PublicoAlvo.Comercial);

        repositorio.Destinatarios.Should().ContainSingle().Which.Should().BeSameAs(destinatario);
    }

    [Fact]
    public async Task Mesmo_email_no_mesmo_publico_e_duplicado_mas_em_outro_publico_e_aceito()
    {
        var repositorio = new FakeDestinatarioRepository()
            .Com(new Destinatario("ACME", "contato@acme.com", PublicoAlvo.Cliente));
        var cadastro = new CadastroDestinatarios(repositorio);

        var duplicar = () => cadastro.CadastrarAsync("Outro", " CONTATO@acme.com", PublicoAlvo.Cliente);
        await duplicar.Should().ThrowAsync<EmailDestinatarioDuplicadoException>();

        await cadastro.CadastrarAsync("ACME suporte", "contato@acme.com", PublicoAlvo.Suporte);
        repositorio.Destinatarios.Should().HaveCount(2);
    }

    [Fact]
    public async Task Alterar_mantendo_o_proprio_email_nao_conta_como_duplicado()
    {
        var existente = new Destinatario("ACME", "contato@acme.com", PublicoAlvo.Cliente);
        var cadastro = new CadastroDestinatarios(new FakeDestinatarioRepository().Com(existente));

        var alterado = await cadastro.AlterarAsync(existente.Id, "ACME Ltda", "contato@acme.com", PublicoAlvo.Cliente);

        alterado.Nome.Should().Be("ACME Ltda");
    }

    [Fact]
    public async Task Alterar_para_email_de_outro_destinatario_do_publico_e_recusado()
    {
        var a = new Destinatario("A", "a@acme.com", PublicoAlvo.Cliente);
        var b = new Destinatario("B", "b@acme.com", PublicoAlvo.Cliente);
        var cadastro = new CadastroDestinatarios(new FakeDestinatarioRepository().Com(a, b));

        var alterar = () => cadastro.AlterarAsync(b.Id, "B", "a@acme.com", PublicoAlvo.Cliente);

        await alterar.Should().ThrowAsync<EmailDestinatarioDuplicadoException>();
        b.Email.Should().Be("b@acme.com");
    }

    [Fact]
    public async Task Operar_sobre_id_inexistente_lanca_nao_encontrado()
    {
        var cadastro = new CadastroDestinatarios(new FakeDestinatarioRepository());

        await cadastro.Invoking(c => c.DesativarAsync(Guid.NewGuid()))
            .Should().ThrowAsync<DestinatarioNaoEncontradoException>();
        await cadastro.Invoking(c => c.ReativarAsync(Guid.NewGuid()))
            .Should().ThrowAsync<DestinatarioNaoEncontradoException>();
        await cadastro.Invoking(c => c.AlterarAsync(Guid.NewGuid(), "x", "x@x.com", PublicoAlvo.Cliente))
            .Should().ThrowAsync<DestinatarioNaoEncontradoException>();
    }
}
