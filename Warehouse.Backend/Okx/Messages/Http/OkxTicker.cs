using System.Text.Json.Serialization;

namespace Warehouse.Backend.Okx.Messages.Http;

public record OkxTicker
{
    [JsonPropertyName("instId")]
    public string InstId { get; init; } = string.Empty;

    [JsonPropertyName("last")]
    public string LastPrice { get; init; } = string.Empty;

    [JsonPropertyName("lastSz")]
    public string LastSize { get; init; } = string.Empty;

    [JsonPropertyName("askPx")]
    public string AskPrice { get; init; } = string.Empty;

    [JsonPropertyName("askSz")]
    public string AskSize { get; init; } = string.Empty;

    [JsonPropertyName("bidPx")]
    public string BidPrice { get; init; } = string.Empty;

    [JsonPropertyName("bidSz")]
    public string BidSize { get; init; } = string.Empty;

    [JsonPropertyName("open24h")]
    public string Open24h { get; init; } = string.Empty;

    [JsonPropertyName("high24h")]
    public string High24h { get; init; } = string.Empty;

    [JsonPropertyName("low24h")]
    public string Low24h { get; init; } = string.Empty;

    [JsonPropertyName("vol24h")]
    public string Volume24h { get; init; } = string.Empty;

    [JsonPropertyName("volCcy24h")]
    public string VolumeCcy24h { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public string Timestamp { get; init; } = string.Empty;
}
