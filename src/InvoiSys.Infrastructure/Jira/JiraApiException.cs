using System.Net;

namespace InvoiSys.Infrastructure.Jira;

/// <summary>
/// Erro de comunicação com a API do Jira, após esgotar as tentativas de retry (quando
/// aplicável). Envelope único para qualquer status HTTP de erro — o chamador (a camada
/// de API) captura só este tipo e nunca precisa saber se a causa raiz foi um 401, 404
/// ou 503.
/// </summary>
public sealed class JiraApiException(string mensagem, HttpStatusCode statusCode)
    : Exception(mensagem)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// 429/5xx são transitórios — a policy de resiliência os re-tenta antes de
    /// desistir. 4xx (token inválido, Release inexistente) nunca é, então uma
    /// tentativa já basta.
    /// </summary>
    public bool Retentavel =>
        StatusCode == HttpStatusCode.TooManyRequests || (int)StatusCode >= 500;
}
