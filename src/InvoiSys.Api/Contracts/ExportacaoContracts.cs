using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

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
