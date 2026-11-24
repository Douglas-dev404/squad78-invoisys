using System.Text.RegularExpressions;
using FluentValidation;
using InvoiSys.Api.Contracts;

namespace InvoiSys.Api.Validacao;

public sealed partial class SalvarJiraInValidator : AbstractValidator<SalvarJiraIn>
{
    public SalvarJiraInValidator()
    {
        RuleFor(e => e.BaseUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .When(e => !string.IsNullOrWhiteSpace(e.BaseUrl))
            .WithMessage("Informe a URL do Jira com https://, por exemplo https://suaempresa.atlassian.net.");
        RuleFor(e => e.Email)
            .EmailAddress().When(e => !string.IsNullOrWhiteSpace(e.Email)).WithMessage("E-mail inválido.");
        RuleFor(e => e.Projeto)
            .Must(p => ChaveProjeto().IsMatch(p!.Trim()))
            .When(e => !string.IsNullOrWhiteSpace(e.Projeto))
            .WithMessage("A chave do projeto usa letras, números e _, como INV.");
        RuleFor(e => e.ApiToken).MaximumLength(2000).WithMessage("Token longo demais.");
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{1,19}$")]
    private static partial Regex ChaveProjeto();
}

public sealed class SalvarIaInValidator : AbstractValidator<SalvarIaIn>
{
    public SalvarIaInValidator()
    {
        RuleFor(e => e.Provedor)
            .Must(p => string.Equals(p, "OpenAi", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p, "OpenRouter", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Use OpenAi ou OpenRouter.");
        RuleFor(e => e.Modelo).MaximumLength(100).WithMessage("Modelo deve ter no máximo 100 caracteres.");
        RuleFor(e => e.ApiKey).MaximumLength(2000).WithMessage("Chave longa demais.");
    }
}
