using System.Text.Json.Serialization;

namespace Warehouse.Backend.Okx.Messages;

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
