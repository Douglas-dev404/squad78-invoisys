using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Validacao;

public sealed class AprovarInValidator : AbstractValidator<AprovarIn>
{
    public AprovarInValidator() => RuleFor(entrada => entrada.Publico).PublicoObrigatorio();
}

public sealed class ReprovarInValidator : AbstractValidator<ReprovarIn>
{
    public ReprovarInValidator()
    {
        RuleFor(entrada => entrada.Publico).PublicoObrigatorio();
        RuleFor(entrada => entrada.Motivo).NotEmpty().WithMessage("Motivo da reprovação é obrigatório.");
    }
}

public sealed class ReabrirInValidator : AbstractValidator<ReabrirIn>
{
    public ReabrirInValidator() => RuleFor(entrada => entrada.Publico).PublicoObrigatorio();
}

internal static class RegrasRevisao
{
    public static IRuleBuilderOptions<T, string?> PublicoObrigatorio<T>(this IRuleBuilder<T, string?> regra) =>
        regra.Must(valor => PublicoAlvoExtensions.TentarConverter(valor, out _))
            .WithMessage("Use cliente, comercial, suporte ou interno.");
}
