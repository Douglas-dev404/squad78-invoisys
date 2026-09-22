namespace InvoiSys.Domain.Enums;

/// <summary>Formatos de exportação do comunicado — Markdown é o único obrigatório do MVP.</summary>
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
