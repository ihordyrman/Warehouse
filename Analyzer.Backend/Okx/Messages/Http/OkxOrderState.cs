using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages.Http;

[JsonConverter(typeof(JsonStringEnumConverter<OkxOrderState>))]
public enum OkxOrderState
{
    [JsonPropertyName("live")]
    Live,
    [JsonPropertyName("partially_filled")]
    PartiallyFilled,
    [JsonPropertyName("filled")]
    Filled,
    [JsonPropertyName("cancelled")]
    Cancelled
}
