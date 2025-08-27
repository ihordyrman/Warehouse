using System.Text.Json.Serialization;

namespace Warehouse.Backend.Okx.Messages.Http;

public record OkxOrder
{
    [JsonPropertyName("instId")]
    public string InstId { get; init; } = string.Empty;

    [JsonPropertyName("ordId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("clOrdId")]
    public string ClientOrderId { get; init; } = string.Empty;

    [JsonPropertyName("side")]
    public OkxOrderSide Side { get; init; }

    [JsonPropertyName("ordType")]
    public OkxOrderType OrderType { get; init; }

    [JsonPropertyName("sz")]
    public string Size { get; init; } = string.Empty;

    [JsonPropertyName("px")]
    public string Price { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public OkxOrderState State { get; init; }

    [JsonPropertyName("accFillSz")]
    public string AccumulatedFillSize { get; init; } = string.Empty;

    [JsonPropertyName("avgPx")]
    public string AveragePrice { get; init; } = string.Empty;

    [JsonPropertyName("cTime")]
    public string CreateTime { get; init; } = string.Empty;

    [JsonPropertyName("uTime")]
    public string UpdateTime { get; init; } = string.Empty;

    [JsonPropertyName("fee")]
    public string Fee { get; init; } = string.Empty;

    [JsonPropertyName("feeCcy")]
    public string FeeCurrency { get; init; } = string.Empty;
}
