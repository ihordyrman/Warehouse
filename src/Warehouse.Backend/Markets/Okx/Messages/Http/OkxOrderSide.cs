using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Http;

[JsonConverter(typeof(JsonStringEnumConverter<OkxOrderSide>))]
public enum OkxOrderSide
{
    [JsonPropertyName("buy")]
    Buy,
    [JsonPropertyName("sell")]
    Sell
}
