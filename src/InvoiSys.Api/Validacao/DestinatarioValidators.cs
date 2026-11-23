using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Validacao;

public sealed class SalvarDestinatarioInValidator : AbstractValidator<SalvarDestinatarioIn>
{
    public SalvarDestinatarioInValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(Destinatario.TamanhoMaximoNome)
            .WithMessage($"Nome deve ter no máximo {Destinatario.TamanhoMaximoNome} caracteres.");
        RuleFor(entrada => entrada.Email).EmailValido();
        RuleFor(entrada => entrada.Publico).PublicoObrigatorio();
    }
}

public sealed class ListarDestinatariosInValidator : AbstractValidator<ListarDestinatariosIn>
{
    public ListarDestinatariosInValidator() =>
        RuleFor(entrada => entrada.Publico)
            .Must(valor => PublicoAlvoExtensions.TentarConverter(valor, out _))
            .When(entrada => !string.IsNullOrWhiteSpace(entrada.Publico))
            .WithMessage("Use cliente, comercial, suporte ou interno.");
}
