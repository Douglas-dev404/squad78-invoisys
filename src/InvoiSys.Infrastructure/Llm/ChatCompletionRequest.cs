using System.Text.Json.Serialization;

namespace InvoiSys.Infrastructure.Llm;

internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

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
