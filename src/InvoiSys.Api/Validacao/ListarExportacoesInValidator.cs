using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Validacao;

public sealed class ListarExportacoesInValidator : AbstractValidator<ListarExportacoesIn>
{
    public const int TamanhoMaximo = 100;

    public ListarExportacoesInValidator()
    {
        RuleFor(entrada => entrada.Pagina)
            .GreaterThanOrEqualTo(1)
            .When(entrada => entrada.Pagina is not null)
            .WithMessage("Página deve ser maior ou igual a 1.");

        RuleFor(entrada => entrada.Tamanho)
            .InclusiveBetween(1, TamanhoMaximo)
            .When(entrada => entrada.Tamanho is not null)
            .WithMessage($"Tamanho deve estar entre 1 e {TamanhoMaximo}.");

        RuleFor(entrada => entrada.Publico)
            .Must(valor => PublicoAlvoExtensions.TentarConverter(valor, out _))
            .When(entrada => !string.IsNullOrWhiteSpace(entrada.Publico))
            .WithMessage("Use cliente, comercial, suporte ou interno.");
    }
}
