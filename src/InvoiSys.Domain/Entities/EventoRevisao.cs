using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class EventoRevisao
{
    private EventoRevisao()
    {
    }

    internal EventoRevisao(
        Guid releaseId,
        Guid versaoId,
        Guid? itemId,
        AcaoRevisao acao,
        Guid usuarioId,
        DateTimeOffset ocorridoEm,
        IReadOnlyDictionary<string, object?>? detalhe)
    {
        Id = Guid.NewGuid();
        ReleaseId = releaseId;
        VersaoId = versaoId;
        ItemId = itemId;
        Acao = acao;
        UsuarioId = usuarioId;
        OcorridoEm = ocorridoEm;
        Detalhe = detalhe;
    }

    public Guid Id { get; private init; }

    public long Sequencia { get; private set; }

    public Guid ReleaseId { get; private init; }

    public Guid VersaoId { get; private init; }

    public Guid? ItemId { get; private init; }

    public AcaoRevisao Acao { get; private init; }

    public Guid UsuarioId { get; private init; }

    public DateTimeOffset OcorridoEm { get; private init; }

    public IReadOnlyDictionary<string, object?>? Detalhe { get; private init; }
}
