using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Api.Contracts;

public sealed record ExportarIn(string? Publico, string? Formato, string? GeradoPor);

public sealed record ComunicadoExportadoOut(
    Guid Id,
    string Publico,
    string Formato,
    string? Conteudo,
    string? GeradoPor,
    DateTimeOffset GeradoEm)
{
    public static ComunicadoExportadoOut De(ComunicadoExportado exportado) => new(
        exportado.Id,
        exportado.Publico.ParaValor(),
        exportado.Formato.ParaValor(),
        exportado.Conteudo,
        exportado.GeradoPor,
        exportado.GeradoEm);
}
