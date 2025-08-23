using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

public record OkxEventResponse
{
    [JsonPropertyName("event")]
    public OkxEvent? Event { get; init; }
}
