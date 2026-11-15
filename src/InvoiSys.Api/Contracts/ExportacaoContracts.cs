using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Api.Contracts;

public sealed record ExportarIn(string? Publico, string? Formato);

public sealed record ComunicadoExportadoOut(
    Guid Id,
    string Publico,
    string Formato,
    string? Conteudo,
    Guid? GeradoPorId,
    string? GeradoPor,
    DateTimeOffset GeradoEm)
{
    public static ComunicadoExportadoOut De(ComunicadoExportado exportado) => new(
        exportado.Id,
        exportado.Publico.ParaValor(),
        exportado.Formato.ParaValor(),
        exportado.Conteudo,
        exportado.GeradoPorId,
        exportado.GeradoPor,
        exportado.GeradoEm);
}

public sealed record ListarExportacoesIn(int? Pagina, int? Tamanho, string? Publico, string? Busca);

public sealed record PaginaOut<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int Tamanho);

public sealed record ExportacaoResumoOut(
    Guid Id,
    string ChaveRelease,
    string? TituloExecutivo,
    string Publico,
    string Formato,
    string? GeradoPor,
    DateTimeOffset GeradoEm)
{
    public static ExportacaoResumoOut De(ExportacaoResumo resumo) => new(
        resumo.Id,
        resumo.ChaveRelease,
        resumo.TituloExecutivo,
        resumo.Publico.ParaValor(),
        resumo.Formato.ParaValor(),
        resumo.GeradoPor,
        resumo.GeradoEm);
}

public sealed record ExportacaoDetalheOut(
    Guid Id,
    string ChaveRelease,
    string? TituloExecutivo,
    string Publico,
    string Formato,
    string? GeradoPor,
    DateTimeOffset GeradoEm,
    string? Conteudo)
{
    public static ExportacaoDetalheOut De(ExportacaoDetalhe detalhe) => new(
        detalhe.Id,
        detalhe.ChaveRelease,
        detalhe.TituloExecutivo,
        detalhe.Publico.ParaValor(),
        detalhe.Formato.ParaValor(),
        detalhe.GeradoPor,
        detalhe.GeradoEm,
        detalhe.Conteudo);
}
