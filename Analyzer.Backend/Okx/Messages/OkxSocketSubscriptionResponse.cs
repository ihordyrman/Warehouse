using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxSocketSubscriptionResponse : OkxSocketEventResponse
{
    [JsonPropertyName("arg")]
    public OkxSocketSubscriptionArgs? Arguments { get; init; }

    [JsonPropertyName("action")]
    public OkxAction? Action { get; init; }

    [JsonPropertyName("data")]
    public OkxSocketBookData[]? Data { get; init; }
}
