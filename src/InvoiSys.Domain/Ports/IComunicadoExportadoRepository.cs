using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Ports;

public interface IComunicadoExportadoRepository
{
    Task AdicionarAsync(ComunicadoExportado exportado, CancellationToken cancellationToken = default);

    Task<ComunicadoExportado?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComunicadoExportado>> ListarPorReleaseAsync(
        Guid releaseId,
        PublicoAlvo? publico = null,
        CancellationToken cancellationToken = default);

    Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default);

    Task<ExportacaoResumo?> BuscarDetalheAsync(Guid id, CancellationToken cancellationToken = default);
}

// Conteudo só vem preenchido no detalhe; a listagem não carrega o Markdown.
public sealed record ExportacaoResumo(
    Guid Id,
    string ChaveRelease,
    string? TituloExecutivo,
    PublicoAlvo Publico,
    FormatoExportacao Formato,
    string? GeradoPor,
    DateTimeOffset GeradoEm,
    string? Conteudo = null);

public sealed record PaginaExportacoes(IReadOnlyList<ExportacaoResumo> Itens, int Total);
