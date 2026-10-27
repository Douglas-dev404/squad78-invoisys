using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class ReleaseNaoAprovadaException(string mensagem) : Exception(mensagem);

public sealed class ComunicadoExportado
{
    private ComunicadoExportado()
    {
    }

    public ComunicadoExportado(
        VersaoComunicado versao,
        FormatoExportacao formato,
        string? conteudo,
        string? caminhoArquivo,
        string? geradoPor)
    {
        if (!versao.ProntaParaExportar)
        {
            throw new ReleaseNaoAprovadaException(
                $"A versão {versao.Publico.ParaValor()} não está aprovada — nenhuma " +
                "exportação é válida antes do gate de revisão humana.");
        }

        Id = Guid.NewGuid();
        ReleaseId = versao.ReleaseId;
        VersaoComunicadoId = versao.Id;
        Publico = versao.Publico;
        Formato = formato;
        Conteudo = conteudo;
        CaminhoArquivo = caminhoArquivo;
        GeradoPor = geradoPor;
        GeradoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public Guid ReleaseId { get; private init; }

    public Guid VersaoComunicadoId { get; private init; }

    public PublicoAlvo Publico { get; private init; }

    public FormatoExportacao Formato { get; private init; }

    public string? Conteudo { get; private init; }

    public string? CaminhoArquivo { get; private init; }

    public string? GeradoPor { get; private init; }

    public DateTimeOffset GeradoEm { get; private init; }
}
