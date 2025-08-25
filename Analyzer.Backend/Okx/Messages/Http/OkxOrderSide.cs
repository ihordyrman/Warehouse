using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages.Http;

[JsonConverter(typeof(JsonStringEnumConverter<OkxOrderSide>))]
public enum OkxOrderSide
{
    [JsonPropertyName("buy")]
    Buy,
    [JsonPropertyName("sell")]
    Sell
}
