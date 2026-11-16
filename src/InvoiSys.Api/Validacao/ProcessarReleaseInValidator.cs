using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Validacao;

public sealed class ProcessarReleaseInValidator : AbstractValidator<ProcessarReleaseIn>
{
    public ProcessarReleaseInValidator()
    {
        RuleFor(entrada => entrada.Publicos)
            .Must(valor => ProcessarReleaseIn.Valores(valor)
                .All(publico => PublicoAlvoExtensions.TentarConverter(publico, out _)))
            .When(entrada => !string.IsNullOrWhiteSpace(entrada.Publicos))
            .WithMessage("Use uma lista separada por vírgula com cliente, comercial, suporte ou interno.");
    }
}
