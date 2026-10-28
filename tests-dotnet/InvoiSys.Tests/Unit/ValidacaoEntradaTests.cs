using FluentAssertions;
using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Api.Validacao;

namespace InvoiSys.Tests.Unit;

public class ValidacaoEntradaTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("financeiro")]
    [InlineData("0")]
    public void Revisao_recusa_publico_ausente_ou_desconhecido(string? publico)
    {
        new AprovarInValidator().Validate(new AprovarIn(publico, "Revisora"))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Publico");
        new ReprovarInValidator().Validate(new ReprovarIn(publico, "Revisar texto", "Revisora"))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Publico");
        new ReabrirInValidator().Validate(new ReabrirIn(publico))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Publico");
    }

    [Theory]
    [InlineData("cliente")]
    [InlineData("comercial")]
    [InlineData("suporte")]
    [InlineData("interno")]
    [InlineData(" CLIENTE ")]
    public void Revisao_aceita_os_publicos_do_dominio(string publico)
    {
        new AprovarInValidator().Validate(new AprovarIn(publico, "Revisora")).IsValid.Should().BeTrue();
        new ReprovarInValidator().Validate(new ReprovarIn(publico, "Revisar texto", "Revisora"))
            .IsValid.Should().BeTrue();
        new ReabrirInValidator().Validate(new ReabrirIn(publico)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Reprovar_exige_motivo_e_editar_exige_texto(string? valor)
    {
        new ReprovarInValidator().Validate(new ReprovarIn("cliente", valor, "Revisora"))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Motivo");
        new EditarItemInValidator().Validate(new EditarItemIn(valor))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Texto");
    }

    [Fact]
    public void Editar_aceita_texto_preenchido() =>
        new EditarItemInValidator().Validate(new EditarItemIn("Texto revisado."))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalido")]
    [InlineData("@example.com")]
    [InlineData("usuario@")]
    public void Regra_email_recusa_ausencia_e_formato_invalido(string? email) =>
        new EntradaEmailValidator().Validate(new EntradaEmail(email))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Email");

    [Fact]
    public void Regra_email_limita_a_320_caracteres_sem_rejeitar_o_limite()
    {
        var validator = new EntradaEmailValidator();

        validator.Validate(new EntradaEmail("usuario@example.com")).IsValid.Should().BeTrue();
        validator.Validate(new EntradaEmail(new string('a', 308) + "@example.com"))
            .IsValid.Should().BeTrue();
        validator.Validate(new EntradaEmail(new string('a', 309) + "@example.com"))
            .Errors.Should().ContainSingle(erro => erro.PropertyName == "Email");
    }

    private sealed record EntradaEmail(string? Email);

    private sealed class EntradaEmailValidator : AbstractValidator<EntradaEmail>
    {
        public EntradaEmailValidator() => RuleFor(entrada => entrada.Email).EmailValido();
    }
}
