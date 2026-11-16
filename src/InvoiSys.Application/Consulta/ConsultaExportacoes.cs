using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed class ConsultaExportacoes(IComunicadoExportadoRepository exportados)
{
    public Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default) =>
        exportados.ListarAsync(publico, busca, pagina, tamanho, cancellationToken);

    public Task<ExportacaoResumo?> BuscarAsync(Guid id, CancellationToken cancellationToken = default) =>
        exportados.BuscarDetalheAsync(id, cancellationToken);
}
