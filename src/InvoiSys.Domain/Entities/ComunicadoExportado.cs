using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

/// <summary>Levantado ao tentar exportar uma versão que ainda não passou pelo gate de aprovação.</summary>
public sealed class ReleaseNaoAprovadaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Uma versão exportada do comunicado de uma Release, para um público e formato
/// específicos — diferencial documentado no enunciado (múltiplos públicos:
/// cliente/comercial/suporte/interno; múltiplos formatos: Markdown/HTML/PDF).
///
/// Nasce de uma <see cref="VersaoComunicado"/> aprovada — é ela, e não a Release,
/// que carrega o gate de revisão por público. Uma mesma versão pode gerar várias
/// linhas aqui (uma por formato, e uma por reexportação) — histórico completo de
/// tudo que já foi publicado, não apenas o último export.
/// </summary>
public sealed class ComunicadoExportado
{
    /// <summary>Uso exclusivo do EF Core para materializar a entidade vinda do banco.</summary>
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

    /// <summary>A versão (público) exata que originou esta exportação.</summary>
    public Guid VersaoComunicadoId { get; private init; }

    /// <summary>Espelha o público da versão — redundante de propósito, facilita consulta.</summary>
    public PublicoAlvo Publico { get; private init; }

    public FormatoExportacao Formato { get; private init; }

    /// <summary>Conteúdo textual (Markdown/HTML) quando aplicável — PDF usa <see cref="CaminhoArquivo"/>.</summary>
    public string? Conteudo { get; private init; }

    /// <summary>Caminho/URL do arquivo gerado, quando o formato é binário (PDF).</summary>
    public string? CaminhoArquivo { get; private init; }

    /// <summary>
    /// Identificação de quem/o que gerou (nome do usuário, ou "sistema" para geração
    /// automática) — texto livre, não FK: autenticação ainda não está implementada no
    /// domínio (ver <see cref="Usuario"/>), mesma decisão aplicada a
    /// <see cref="VersaoComunicado.RevisadoPor"/>.
    /// </summary>
    public string? GeradoPor { get; private init; }

    public DateTimeOffset GeradoEm { get; private init; }
}
