using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Infrastructure.Llm;

public sealed class ProviderPendente(string chaveConfiguracao = "OpenRouter:ApiKey") : ILlmProvider
{
    private readonly string _mensagem =
        $"Nenhum LLM provider configurado — defina {chaveConfiguracao} (ou a variável de "
        + $"ambiente {chaveConfiguracao.Replace(":", "__")}) para habilitar o pipeline de IA.";

    public Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(_mensagem);

    public Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(_mensagem);

    public Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(_mensagem);

    public Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        CancellationToken cancellationToken = default) =>
        throw new ProviderNaoConfiguradoException(_mensagem);
}
