using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxSocketSubscriptionResponse
{
    [JsonPropertyName("arg")]
    public OkxSocketSubscriptionArgs? Arguments { get; init; }

    [JsonPropertyName("action")]
    public OkxAction? Action { get; init; }

    public OkxSocketBookData[]? Data { get; init; }
}
