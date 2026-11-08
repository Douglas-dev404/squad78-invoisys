using FluentValidation;

namespace InvoiSys.Api.Validacao;

public static class RegraEmail
{
    public static IRuleBuilderOptions<T, string?> EmailValido<T>(this IRuleBuilderInitial<T, string?> regra) =>
        regra.Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(320).WithMessage("E-mail deve ter no máximo 320 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");
}
