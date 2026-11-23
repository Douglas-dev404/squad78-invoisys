using InvoiSys.Application.Consulta;
using InvoiSys.Application.Exportacao;
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

public sealed record ListarExportacoesIn(int? Pagina, int? Tamanho, string? Publico, string? Busca)
{
    public const int PaginaPadrao = 1;
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;
}

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
    string? Conteudo,
    IReadOnlyList<EnvioOut> Envios)
{
    public static ExportacaoDetalheOut De(ExportacaoComEnvios detalhe) => new(
        detalhe.Exportacao.Id,
        detalhe.Exportacao.ChaveRelease,
        detalhe.Exportacao.TituloExecutivo,
        detalhe.Exportacao.Publico.ParaValor(),
        detalhe.Exportacao.Formato.ParaValor(),
        detalhe.Exportacao.GeradoPor,
        detalhe.Exportacao.GeradoEm,
        detalhe.Exportacao.Conteudo,
        [.. detalhe.Envios.Select(EnvioOut.De)]);
}

public sealed record EnviarIn(string? Publico, IReadOnlyList<Guid>? DestinatarioIds);

public sealed record EnvioOut(Guid DestinatarioId, string Nome, string Email, string Status, string? Erro)
{
    public static EnvioOut De(EnvioComunicado envio) => new(
        envio.DestinatarioId, envio.Nome, envio.Email, envio.Status.ParaValor(), envio.Erro);
}

public sealed record EnvioRealizadoOut(
    Guid ExportacaoId,
    string Publico,
    string? EnviadoPor,
    DateTimeOffset EnviadoEm,
    IReadOnlyList<EnvioOut> Envios)
{
    public static EnvioRealizadoOut De(ResultadoEnvio resultado) => new(
        resultado.Exportacao.Id,
        resultado.Exportacao.Publico.ParaValor(),
        resultado.Exportacao.GeradoPor,
        resultado.Exportacao.GeradoEm,
        [.. resultado.Envios.Select(EnvioOut.De)]);
}
