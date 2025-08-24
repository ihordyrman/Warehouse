using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<OkxAction>))]
public enum OkxAction
{
    [JsonPropertyName("snapshot")]
    Snapshot,
    [JsonPropertyName("event")]
    Event,
    [JsonPropertyName("update")]
    Update
}
