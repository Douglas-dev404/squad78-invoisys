using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface ILlmProvider
{
    Task<CategoriaAlteracao> CategorizarAsync(
        string textoFonte,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyList<string>>> AgruparSemelhantesAsync(
        IReadOnlyList<(string Chave, string Texto)> textos,
        CancellationToken cancellationToken = default);

    Task<string> ReescreverLinguagemNegocioAsync(
        IReadOnlyList<string> textosFonte,
        CategoriaAlteracao categoria,
        PublicoAlvo publico = PublicoAlvo.Cliente,
        CancellationToken cancellationToken = default);

    Task<(string Titulo, string Resumo)> GerarTituloEResumoAsync(
        IReadOnlyList<string> itensTexto,
        PublicoAlvo publico = PublicoAlvo.Cliente,
        CancellationToken cancellationToken = default);
}
