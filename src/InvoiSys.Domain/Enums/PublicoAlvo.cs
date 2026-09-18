namespace InvoiSys.Domain.Enums;

/// <summary>
/// Públicos possíveis para uma exportação de comunicado — diferencial documentado
/// no enunciado do desafio (geração de versões distintas do mesmo comunicado por
/// audiência). Fixo por decisão de negócio, mesmo espírito de
/// <see cref="CategoriaAlteracao"/>: não adicionar público novo sem necessidade
/// real validada.
/// </summary>
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

    public static string ParaValor(this PublicoAlvo publico) => PorEnum[publico];
}
