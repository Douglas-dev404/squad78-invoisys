using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

/// <summary>
/// Porta que qualquer provider de LLM precisa implementar.
///
/// Este é o contrato que desacopla o pipeline de IA do provider concreto. O domínio e
/// os serviços de aplicação dependem só desta interface — nunca de um SDK específico
/// diretamente. Trocar de provider é escrever um adapter novo em
/// InvoiSys.Infrastructure.Llm e mudar um binding na composition root; nada no domínio
/// ou nos services muda.
///
/// Cada método corresponde a um estágio do pipeline de 5 estágios (ver documentação
/// interna de Regras de Negócio) que precisa de fato de uma chamada ao modelo.
/// Extração e Limpeza (estágio 1) é normalização de texto puro, não chama LLM — fica
/// na camada de aplicação.
/// </summary>
public interface ILlmProvider
{
    /// <summary>Estágio 2: classifica um texto em uma das categorias fixas de negócio.</summary>
    Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estágio 3: recebe pares (chaveJira, textoFonte) e retorna grupos de chaves que
    /// devem ser fundidas em um único item de comunicado por tratarem do mesmo
    /// assunto. Cada chave de entrada aparece em exatamente um grupo de saída (grupos
    /// de tamanho 1 = história que não tem duplicata).
    /// </summary>
    Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estágio 4: funde um grupo de textos técnicos em um único parágrafo de
    /// comunicado em linguagem de negócio, coerente com a categoria.
    /// </summary>
    Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estágio 5: gera título e resumo executivos para a Release inteira, a partir dos
    /// itens de comunicado já prontos.
    /// </summary>
    Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        CancellationToken cancellationToken = default);
}
