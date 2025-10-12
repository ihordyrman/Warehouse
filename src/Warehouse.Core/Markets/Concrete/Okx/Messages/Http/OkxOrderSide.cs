using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

[JsonConverter(typeof(JsonStringEnumConverter<OkxOrderSide>))]
public enum OkxOrderSide
{
    [JsonPropertyName("buy")]
    Buy,
    [JsonPropertyName("sell")]
    Sell
}
