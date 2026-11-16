using FluentValidation;
using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Validacao;

public sealed class ListarExportacoesInValidator : AbstractValidator<ListarExportacoesIn>
{
    public ListarExportacoesInValidator()
    {
        RuleFor(entrada => entrada.Pagina)
            .GreaterThanOrEqualTo(1).When(entrada => entrada.Pagina is not null)
            .WithMessage("A página começa em 1.");
        RuleFor(entrada => entrada.Tamanho)
            .InclusiveBetween(1, ListarExportacoesIn.TamanhoMaximo).When(entrada => entrada.Tamanho is not null)
            .WithMessage($"O tamanho da página vai de 1 a {ListarExportacoesIn.TamanhoMaximo}.");
        RuleFor(entrada => entrada.Publico)
            .Must(valor => PublicoAlvoExtensions.TentarConverter(valor, out _))
            .When(entrada => !string.IsNullOrWhiteSpace(entrada.Publico))
            .WithMessage("Use cliente, comercial, suporte ou interno.");
        RuleFor(entrada => entrada.Busca)
            .MaximumLength(200).WithMessage("A busca deve ter no máximo 200 caracteres.");
    }
}
