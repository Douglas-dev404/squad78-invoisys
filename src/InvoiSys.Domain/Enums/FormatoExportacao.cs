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

    public static string ParaValor(this FormatoExportacao formato) => PorEnum[formato];
}
