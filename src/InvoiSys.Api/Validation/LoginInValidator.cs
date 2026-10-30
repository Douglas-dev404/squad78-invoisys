using FluentValidation;
using InvoiSys.Api.Contracts;

namespace InvoiSys.Api.Validation;

public sealed class LoginInValidator : AbstractValidator<LoginIn>
{
    public LoginInValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(254).WithMessage("E-mail longo demais.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MaximumLength(256).WithMessage("Senha longa demais.");
    }
}