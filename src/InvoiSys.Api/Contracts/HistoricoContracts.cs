using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;

namespace InvoiSys.Api.Contracts;

public sealed record UsuarioResumoOut(Guid Id, string Nome);

public sealed record EventoRevisaoOut(
    Guid Id,
    string Acao,
    string Publico,
    Guid? ItemId,
    UsuarioResumoOut Usuario,
    DateTimeOffset OcorridoEm,
    IReadOnlyDictionary<string, object?>? Detalhe)
{
    public static EventoRevisaoOut De(EventoRevisaoResumo evento) => new(
        evento.Id,
        evento.Acao.ParaValor(),
        evento.Publico.ParaValor(),
        evento.ItemId,
        new UsuarioResumoOut(evento.UsuarioId, evento.UsuarioNome),
        evento.OcorridoEm,
        evento.Detalhe);
}
