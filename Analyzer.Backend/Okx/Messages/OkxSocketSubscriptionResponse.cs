using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxSocketSubscriptionResponse
{
    [JsonPropertyName("args")]
    public OkxSocketSubscriptionArgs? Arguments { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }
}
