using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Exportacao;

public sealed class FormatoNaoSuportadoException(FormatoExportacao formato)
    : Exception($"Formato '{formato.ParaValor()}' ainda não é suportado. Use markdown.");

public sealed class ExportacaoComunicado(
    IReleaseRepository releases,
    IComunicadoExportadoRepository exportados)
{
    public const string GeradorPadrao = "sistema";

    public async Task<ComunicadoExportado> ExportarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        FormatoExportacao formato,
        string? geradoPor,
        CancellationToken cancellationToken = default)
    {
        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        var versao = release.VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {chaveRelease} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        if (formato != FormatoExportacao.Markdown)
        {
            throw new FormatoNaoSuportadoException(formato);
        }

        var exportado = new ComunicadoExportado(
            versao,
            formato,
            RenderizadorMarkdown.Renderizar(versao),
            null,
            string.IsNullOrWhiteSpace(geradoPor) ? GeradorPadrao : geradoPor.Trim());

        await exportados.AdicionarAsync(exportado, cancellationToken);

        return exportado;
    }
}
