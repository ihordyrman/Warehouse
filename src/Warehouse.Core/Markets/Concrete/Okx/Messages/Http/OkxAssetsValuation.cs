using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

public record OkxAssetsValuation
{
    [JsonPropertyName("details")]
    public OkxAssetsValuationDetail Details { get; init; } = new();

    [JsonPropertyName("totalBal")]
    public string TotalBalance { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public string Timestamp { get; init; } = string.Empty;
}
