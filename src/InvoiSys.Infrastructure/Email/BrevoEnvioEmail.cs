using System.Net;
using System.Net.Http.Json;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Email;

public sealed class BrevoEnvioEmail(
    HttpClient http,
    IOptions<EmailOptions> options,
    ILogger<BrevoEnvioEmail> logger) : IEnvioEmail
{
    private readonly EmailOptions _opcoes = options.Value;

    public Task EnviarRedefinicaoSenhaAsync(
        string email,
        string nome,
        string link,
        DateTimeOffset expiraEm,
        CancellationToken cancellationToken = default)
    {
        var validade = expiraEm.ToString("dd/MM/yyyy HH:mm 'UTC'");
        var texto =
            $"Olá, {nome}.\n\nRecebemos um pedido para redefinir a sua senha do InvoiSys.\n"
            + $"Use o link abaixo até {validade}:\n\n{link}\n\n"
            + "Se não foi você, ignore este e-mail: a senha atual continua valendo.";
        var html =
            $"<p>Olá, {WebUtility.HtmlEncode(nome)}.</p>"
            + "<p>Recebemos um pedido para redefinir a sua senha do InvoiSys.</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Definir nova senha</a> (válido até {validade}).</p>"
            + "<p>Se não foi você, ignore este e-mail: a senha atual continua valendo.</p>";

        return EnviarAsync(email, nome, "Redefinição de senha do InvoiSys", html, texto, cancellationToken);
    }

    public Task EnviarComunicadoAsync(
        string email,
        string nome,
        string assunto,
        string corpoHtml,
        string corpoTexto,
        CancellationToken cancellationToken = default) =>
        EnviarAsync(email, nome, assunto, corpoHtml, corpoTexto, cancellationToken);

    private async Task EnviarAsync(
        string email,
        string nome,
        string assunto,
        string html,
        string texto,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            sender = new { name = _opcoes.NomeRemetente, email = _opcoes.Remetente },
            to = new[] { new { email, name = nome } },
            subject = assunto,
            htmlContent = html,
            textContent = texto,
        };

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.PostAsJsonAsync(_opcoes.Endpoint, payload, cancellationToken);
        }
        catch (Exception exc) when (exc is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exc, "Falha de rede ao enviar e-mail pela Brevo");
            throw new EnvioEmailException("Provedor de e-mail indisponível.", exc);
        }

        using (resposta)
        {
            if (resposta.IsSuccessStatusCode)
            {
                return;
            }

            var status = (int)resposta.StatusCode;
            var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            corpo = corpo.Length > 500 ? corpo[..500] : corpo;

            logger.LogError(
                "Brevo respondeu erro {Status}: {Corpo}",
                status,
                status is 401 or 403 ? "(corpo omitido: pode ecoar a credencial)" : corpo);

            // Corpo da resposta só no log: esta mensagem chega a quem chamou a API.
            throw new EnvioEmailException($"Provedor de e-mail retornou {status}.");
        }
    }
}
