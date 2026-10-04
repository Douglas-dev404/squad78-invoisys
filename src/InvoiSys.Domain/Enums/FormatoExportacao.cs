namespace InvoiSys.Domain.Enums;

public enum FormatoExportacao
{
    Markdown,
    Html,
    Pdf,
}

public static class FormatoExportacaoExtensions
{
    private static readonly Dictionary<FormatoExportacao, string> PorEnum = new()
    {
        [FormatoExportacao.Markdown] = "markdown",
        [FormatoExportacao.Html] = "html",
        [FormatoExportacao.Pdf] = "pdf",
    };

    private static readonly Dictionary<string, FormatoExportacao> PorValor =
        PorEnum.ToDictionary(par => par.Value, par => par.Key);

    public static string ParaValor(this FormatoExportacao formato) => PorEnum[formato];

    public static bool TentarConverter(string? valor, out FormatoExportacao formato)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            formato = default;
            return false;
        }

        return PorValor.TryGetValue(valor.Trim().ToLowerInvariant(), out formato);
    }
}
