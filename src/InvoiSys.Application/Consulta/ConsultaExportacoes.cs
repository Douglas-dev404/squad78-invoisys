using InvoiSys.Domain.Entities;
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

    public async Task<ExportacaoDetalhe> DetalharAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await exportados.BuscarDetalheAsync(id, cancellationToken)
            ?? throw new ExportacaoNaoEncontradaException(id);
}
