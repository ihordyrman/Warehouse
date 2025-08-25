using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxSocketResponse : OkxSocketEventResponse
{
    [JsonPropertyName("arg")]
    public OkxSocketArgs? Arguments { get; init; }

    [JsonPropertyName("action")]
    public OkxAction? Action { get; init; }

    [JsonPropertyName("data")]
    public OkxSocketBookData[]? Data { get; init; }
}
