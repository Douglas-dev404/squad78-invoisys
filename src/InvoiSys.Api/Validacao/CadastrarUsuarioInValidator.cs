using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Entities;

namespace InvoiSys.Api.Validacao;

public sealed class CadastrarUsuarioInValidator : AbstractValidator<CadastrarUsuarioIn>
{
    public CadastrarUsuarioInValidator()
    {
        RuleFor(entrada => entrada.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");
        RuleFor(entrada => entrada.Email).EmailValido();
        RuleFor(entrada => entrada.Papel)
            .Must(Usuario.PapelEhValido).WithMessage("Use admin ou release_manager.");
        RuleFor(entrada => entrada.SenhaInicial)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha inicial é obrigatória.")
            .MinimumLength(8).WithMessage("Senha inicial deve ter pelo menos 8 caracteres.");
    }
}
