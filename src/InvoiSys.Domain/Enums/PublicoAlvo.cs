namespace InvoiSys.Domain.Enums;

public enum PublicoAlvo
{
    Cliente,
    Comercial,
    Suporte,
    Interno,
}

public static class PublicoAlvoExtensions
{
    private static readonly Dictionary<PublicoAlvo, string> PorEnum = new()
    {
        [PublicoAlvo.Cliente] = "cliente",
        [PublicoAlvo.Comercial] = "comercial",
        [PublicoAlvo.Suporte] = "suporte",
        [PublicoAlvo.Interno] = "interno",
    };

    private static readonly Dictionary<string, PublicoAlvo> PorValor =
        PorEnum.ToDictionary(par => par.Value, par => par.Key);

    public static string ParaValor(this PublicoAlvo publico) => PorEnum[publico];

    public static bool TentarConverter(string? valor, out PublicoAlvo publico)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            publico = default;
            return false;
        }

        return PorValor.TryGetValue(valor.Trim().ToLowerInvariant(), out publico);
    }
}
