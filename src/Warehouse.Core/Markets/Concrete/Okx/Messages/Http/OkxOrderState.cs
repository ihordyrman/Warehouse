using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

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
