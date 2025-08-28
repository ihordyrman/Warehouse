using System.Text.Json.Serialization;

namespace Warehouse.Backend.Features.Okx.Messages.Http;

public record OkxOrderBook
{
    [JsonPropertyName("asks")]
    public string[][] Asks { get; init; } = [];

    [JsonPropertyName("bids")]
    public string[][] Bids { get; init; } = [];

    [JsonPropertyName("ts")]
    public string Timestamp { get; init; } = string.Empty;
}
