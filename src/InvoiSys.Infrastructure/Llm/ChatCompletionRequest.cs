using System.Text.Json.Serialization;

namespace InvoiSys.Infrastructure.Llm;

/// <summary>
/// Corpo da requisição de chat completions (schema OpenAI, que a OpenRouter espelha).
///
/// Existe como tipo concreto de propósito: um <c>Dictionary&lt;string, object&gt;</c> é
/// serializado pelo System.Text.Json segundo o tipo DECLARADO de cada valor, então
/// qualquer objeto guardado como <c>object</c> sai como <c>{}</c> — na prática, o
/// prompt iria vazio para o modelo e nada acusaria o erro.
///
/// Campos opcionais são nulos por padrão e omitidos da serialização, para o corpo
/// enviado conter só o que foi de fato configurado.
/// </summary>
internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    /// <summary>
    /// Lista de fallback nativa da OpenRouter: se o primeiro modelo falhar com 5xx/429,
    /// o gateway tenta o próximo sem round-trip nosso. Omitido quando não configurado.
    /// </summary>
    [JsonPropertyName("models")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Models { get; init; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? ResponseFormat { get; init; }
}

internal sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

internal sealed class ResponseFormat
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }
}
