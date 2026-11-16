using InvoiSys.Application.Revisao;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Application.Exportacao;

public sealed class FormatoNaoSuportadoException(FormatoExportacao formato)
    : Exception($"Formato '{formato.ParaValor()}' ainda não é suportado. Use markdown ou html.");

public sealed class ExportacaoComunicado(
    IReleaseRepository releases,
    IComunicadoExportadoRepository exportados,
    IUsuarioRepository usuarios)
{
    public async Task<ComunicadoExportado> ExportarAsync(
        string chaveRelease,
        PublicoAlvo publico,
        FormatoExportacao formato,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var gerador = await UsuarioResponsavel.ObterAtivoAsync(usuarios, usuarioId, cancellationToken);

        var release = await releases.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken)
            ?? throw new ReleaseNaoEncontradaException(chaveRelease);

        var versao = release.VersaoPara(publico)
            ?? throw new ReleaseSemItensProcessadosException(
                $"Release {chaveRelease} não tem comunicado gerado para o público "
                + $"{publico.ParaValor()}.");

        var conteudo = formato switch
        {
            FormatoExportacao.Markdown => RenderizadorMarkdown.Renderizar(versao),
            FormatoExportacao.Html => RenderizadorHtml.Renderizar(versao),
            _ => throw new FormatoNaoSuportadoException(formato),
        };

        var exportado = new ComunicadoExportado(
            versao,
            formato,
            conteudo,
            null,
            gerador.Nome,
            gerador.Id);

        await exportados.AdicionarAsync(exportado, cancellationToken);

        return exportado;
    }
}
