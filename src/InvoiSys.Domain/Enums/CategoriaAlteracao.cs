namespace InvoiSys.Domain.Enums;

public enum CategoriaAlteracao
{
    NovaFuncionalidade,
    Melhoria,
    Correcao,
    Outros,
}

public static class CategoriaAlteracaoExtensions
{
    private static readonly Dictionary<CategoriaAlteracao, string> PorEnum = new()
    {
        [CategoriaAlteracao.NovaFuncionalidade] = "nova_funcionalidade",
        [CategoriaAlteracao.Melhoria] = "melhoria",
        [CategoriaAlteracao.Correcao] = "correcao",
        [CategoriaAlteracao.Outros] = "outros",
    };

    private static readonly Dictionary<string, CategoriaAlteracao> PorValor =
        PorEnum.ToDictionary(par => par.Value, par => par.Key);

    public static string ParaValor(this CategoriaAlteracao categoria) => PorEnum[categoria];

    public static bool TentarConverter(string valor, out CategoriaAlteracao categoria) =>
        PorValor.TryGetValue(valor.Trim().ToLowerInvariant(), out categoria);
}
