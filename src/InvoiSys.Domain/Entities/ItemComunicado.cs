using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>
/// Um item já processado pela IA: uma ou mais histórias do Jira condensadas em um
/// único parágrafo de comunicado, já categorizado e em linguagem de negócio.
///
/// Um ItemComunicado pode se originar de várias HistoriaJira (agrupamento semântico —
/// estágio 3 do pipeline, ver documentação interna de Regras de Negócio) — por isso
/// <see cref="Origens"/> é lista, nunca uma chave única.
/// </summary>
public sealed class ItemComunicado
{
    public ItemComunicado(CategoriaAlteracao categoria, string texto, IReadOnlyList<string> origens)
    {
        Categoria = categoria;
        Texto = texto;
        Origens = origens;
    }

    public CategoriaAlteracao Categoria { get; }

    /// <summary>Já em linguagem de negócio, pronto pro cliente ler.</summary>
    public string Texto { get; }

    /// <summary>Chaves Jira que originaram este item (ex: ["INV-1234", "INV-1240"]).</summary>
    public IReadOnlyList<string> Origens { get; }

    public string? TextoEditadoManualmente { get; private set; }

    /// <summary>
    /// Texto que efetivamente vai pro comunicado publicado: a edição humana
    /// sobrescreve o texto gerado pela IA quando existir. Nunca o contrário — revisão
    /// humana é a última palavra (invariante de negócio).
    /// </summary>
    public string TextoFinal =>
        string.IsNullOrWhiteSpace(TextoEditadoManualmente) ? Texto : TextoEditadoManualmente;

    public void EditarManualmente(string texto) => TextoEditadoManualmente = texto;
}
