using InvoiSys.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Infrastructure.Email;

public sealed class EnvioEmailPendente(ILogger<EnvioEmailPendente> logger) : IEnvioEmail
{
    public Task EnviarRedefinicaoSenhaAsync(
        string email,
        string nome,
        string link,
        DateTimeOffset expiraEm,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Envio de e-mail não configurado (Email__ApiKey pendente). Link de redefinição de senha "
            + "para {Email}, válido até {ExpiraEm:O}: {Link}",
            email,
            expiraEm,
            link);

        return Task.CompletedTask;
    }

    public Task EnviarComunicadoAsync(
        string email,
        string nome,
        string assunto,
        string corpoHtml,
        string corpoTexto,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Envio de e-mail não configurado (Email__ApiKey pendente). Comunicado \"{Assunto}\" "
            + "seria enviado para {Email}",
            assunto,
            email);

        return Task.CompletedTask;
    }
}
