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
            "Envio de e-mail não configurado (SMTP pendente). Link de redefinição de senha "
            + "para {Email}, válido até {ExpiraEm:O}: {Link}",
            email,
            expiraEm,
            link);

        return Task.CompletedTask;
    }
}
