using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxSocketSubscriptionArgs
{
    [JsonPropertyName("channel")]
    public string? Channel { get; init; }

    [JsonPropertyName("instId")]
    public string? InstrumentId { get; set; }
}
