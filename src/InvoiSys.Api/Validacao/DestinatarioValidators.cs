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

public sealed class EnviarInValidator : AbstractValidator<EnviarIn>
{
    public const int MaximoDestinatarios = 100;

    public EnviarInValidator()
    {
        RuleFor(entrada => entrada.Publico).PublicoObrigatorio();
        RuleFor(entrada => entrada.DestinatarioIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Escolha pelo menos um destinatário.")
            .Must(ids => ids!.Count <= MaximoDestinatarios)
            .WithMessage($"No máximo {MaximoDestinatarios} destinatários por envio.")
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .WithMessage("A lista de destinatários tem id repetido.");
    }
}
