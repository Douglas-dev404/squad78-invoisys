using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Entities;

public sealed class EnvioComunicado
{
    public const int TamanhoMaximoErro = 500;

    private EnvioComunicado()
    {
        Nome = string.Empty;
        Email = string.Empty;
    }

    private EnvioComunicado(
        ComunicadoExportado exportado,
        Destinatario destinatario,
        StatusEnvio status,
        string? erro)
    {
        if (destinatario.Publico != exportado.Publico)
        {
            throw new ArgumentException(
                $"O destinatário {destinatario.Email} é do público {destinatario.Publico.ParaValor()}, "
                + $"não {exportado.Publico.ParaValor()}.",
                nameof(destinatario));
        }

        Id = Guid.NewGuid();
        ComunicadoExportadoId = exportado.Id;
        DestinatarioId = destinatario.Id;
        Nome = destinatario.Nome;
        Email = destinatario.Email;
        Status = status;
        Erro = erro is { Length: > TamanhoMaximoErro } ? erro[..TamanhoMaximoErro] : erro;
        EnviadoEm = DateTimeOffset.UtcNow;
    }

    public static EnvioComunicado Enviado(ComunicadoExportado exportado, Destinatario destinatario) =>
        new(exportado, destinatario, StatusEnvio.Enviado, null);

    public static EnvioComunicado Falhou(ComunicadoExportado exportado, Destinatario destinatario, string erro) =>
        new(exportado, destinatario, StatusEnvio.Falhou, erro);

    public Guid Id { get; private init; }

    public Guid ComunicadoExportadoId { get; private init; }

    public Guid DestinatarioId { get; private init; }

    public string Nome { get; private init; }

    public string Email { get; private init; }

    public StatusEnvio Status { get; private init; }

    public string? Erro { get; private init; }

    public DateTimeOffset EnviadoEm { get; private init; }
}
