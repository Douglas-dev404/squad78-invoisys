namespace InvoiSys.Domain.Enums;

/// <summary>
/// Categorias fixas de alteração usadas para classificar cada história da Release.
///
/// Fixas por decisão de negócio — ver documentação interna de Regras de Negócio.
/// Não adicionar categoria nova sem necessidade real validada com o responsável
/// técnico do projeto.
///
/// O valor serializado (o "nova_funcionalidade" que sai na API e que o LLM devolve)
/// vive em <see cref="CategoriaAlteracaoExtensions"/>, não no nome do membro — assim
/// o C# mantém PascalCase idiomático sem quebrar o contrato herdado do Python.
/// </summary>
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

    /// <summary>
    /// Converte o valor textual de volta para o enum. Case-insensitive porque a
    /// origem é resposta de LLM, não entrada controlada.
    /// </summary>
    public static bool TentarConverter(string valor, out CategoriaAlteracao categoria) =>
        PorValor.TryGetValue(valor.Trim().ToLowerInvariant(), out categoria);
}
