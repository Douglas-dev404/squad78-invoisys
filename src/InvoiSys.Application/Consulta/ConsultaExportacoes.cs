using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Consulta;

public sealed record ExportacaoComEnvios(ExportacaoResumo Exportacao, IReadOnlyList<EnvioComunicado> Envios);

public sealed class ConsultaExportacoes(
    IComunicadoExportadoRepository exportados,
    IEnvioComunicadoRepository envios)
{
    public Task<PaginaExportacoes> ListarAsync(
        PublicoAlvo? publico,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default) =>
        exportados.ListarAsync(publico, busca, pagina, tamanho, cancellationToken);

    public async Task<ExportacaoComEnvios?> BuscarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var detalhe = await exportados.BuscarDetalheAsync(id, cancellationToken);
        if (detalhe is null)
        {
            return null;
        }

        return new ExportacaoComEnvios(detalhe, await envios.ListarPorExportacaoAsync(id, cancellationToken));
    }
}
