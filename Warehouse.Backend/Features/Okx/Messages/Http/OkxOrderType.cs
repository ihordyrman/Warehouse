using System.Text.Json.Serialization;

namespace Warehouse.Backend.Features.Okx.Messages.Http;

[JsonConverter(typeof(JsonStringEnumConverter<OkxOrderType>))]
public enum OkxOrderType
{
    [JsonPropertyName("market")]
    Market,
    [JsonPropertyName("limit")]
    Limit,
    [JsonPropertyName("post_only")]
    PostOnly,
    [JsonPropertyName("fok")]
    Fok,
    [JsonPropertyName("ioc")]
    Ioc
}
