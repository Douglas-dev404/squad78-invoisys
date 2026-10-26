namespace InvoiSys.Domain.Ports;

public interface IEnvioEmail
{
    Task EnviarRedefinicaoSenhaAsync(
        string email,
        string nome,
        string link,
        DateTimeOffset expiraEm,
        CancellationToken cancellationToken = default);
}
