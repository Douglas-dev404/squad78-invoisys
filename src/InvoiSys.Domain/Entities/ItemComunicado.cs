using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>
/// Um item já processado pela IA: uma ou mais histórias do Jira condensadas em um
/// único parágrafo de comunicado, já categorizado e em linguagem de negócio.
///
/// Um ItemComunicado pode se originar de várias HistoriaJira (agrupamento semântico —
/// estágio 3 do pipeline, ver documentação interna de Regras de Negócio) — por isso
/// <see cref="Origens"/> é lista, nunca uma chave única.
///
/// Pertence a uma <see cref="VersaoComunicado"/> (um público), não diretamente à
/// Release: o mesmo conjunto de histórias gera textos diferentes para Cliente e para
/// Suporte, e cada um é revisado em separado.
/// </summary>
public sealed class ItemComunicado
{
    /// <summary>Uso exclusivo do EF Core para materializar a entidade vinda do banco.</summary>
    private ItemComunicado()
    {
        Texto = string.Empty;
        Origens = [];
    }

    public ItemComunicado(CategoriaAlteracao categoria, string texto, IReadOnlyList<string> origens)
    {
        Id = Guid.NewGuid();
        Categoria = categoria;
        Texto = texto;
        Origens = origens;
        Incluido = true;
    }

    /// <summary>Identidade técnica — chave estável para FK.</summary>
    public Guid Id { get; private init; }

    public CategoriaAlteracao Categoria { get; private init; }

    /// <summary>Já em linguagem de negócio, pronto pro cliente ler.</summary>
    public string Texto { get; private init; }

    /// <summary>Chaves Jira que originaram este item (ex: ["INV-1234", "INV-1240"]).</summary>
    public IReadOnlyList<string> Origens { get; private init; }

    public string? TextoEditadoManualmente { get; private set; }

    /// <summary>
    /// Se este item entra no comunicado publicado. Nasce <c>true</c>: o revisor exclui
    /// o que não é relevante para a audiência, não seleciona o que é.
    ///
    /// Existe porque o problema original que o produto resolve é justamente "alguém lê
    /// todas as histórias e filtra o que interessa ao cliente" — sem isso, a revisão
    /// humana só permitiria reescrever texto, nunca descartar um item interno que não
    /// deveria chegar ao cliente.
    /// </summary>
    public bool Incluido { get; private set; } = true;

    /// <summary>Justificativa de por que o item foi tirado do comunicado, quando houver.</summary>
    public string? MotivoExclusao { get; private set; }

    /// <summary>
    /// Texto que efetivamente vai pro comunicado publicado: a edição humana
    /// sobrescreve o texto gerado pela IA quando existir. Nunca o contrário — revisão
    /// humana é a última palavra (invariante de negócio).
    /// </summary>
    public string TextoFinal =>
        string.IsNullOrWhiteSpace(TextoEditadoManualmente) ? Texto : TextoEditadoManualmente;

    public void EditarManualmente(string texto) => TextoEditadoManualmente = texto;

    /// <summary>
    /// Tira o item do comunicado sem apagar o registro — o que a IA gerou continua
    /// auditável, junto do motivo pelo qual um humano decidiu não publicá-lo.
    /// </summary>
    public void Excluir(string? motivo = null)
    {
        Incluido = false;
        MotivoExclusao = motivo;
    }

    public void Reincluir()
    {
        Incluido = true;
        MotivoExclusao = null;
    }
}
