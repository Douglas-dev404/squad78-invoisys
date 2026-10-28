using FluentValidation;
using InvoiSys.Api.Contracts;

namespace InvoiSys.Api.Validacao;

public sealed class EditarItemInValidator : AbstractValidator<EditarItemIn>
{
    public EditarItemInValidator() =>
        RuleFor(entrada => entrada.Texto).NotEmpty().WithMessage("Texto da edição é obrigatório.");
}
