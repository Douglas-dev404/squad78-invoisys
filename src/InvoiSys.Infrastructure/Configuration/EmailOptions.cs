namespace InvoiSys.Infrastructure.Configuration;

public sealed class EmailOptions
{
    public const string SecaoConfig = "Email";

    public string ApiKey { get; set; } = string.Empty;

    public string Remetente { get; set; } = string.Empty;

    public string NomeRemetente { get; set; } = "InvoiSys";

    public string Endpoint { get; set; } = "https://api.brevo.com/v3/smtp/email";

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Remetente);
}
