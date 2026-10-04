using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Infrastructure.Llm;

public sealed class ProviderPendente : ILlmProvider
{
    private const string Mensagem =
        "Nenhum LLM provider configurado — defina OpenRouter:ApiKey (ou a variável de "
        + "ambiente OpenRouter__ApiKey) para habilitar o pipeline de IA.";

    public Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);

    public Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);

    public Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);

    public Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(Mensagem);
}
