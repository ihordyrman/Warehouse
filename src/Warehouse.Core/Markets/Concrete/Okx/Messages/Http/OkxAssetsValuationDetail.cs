using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

public record OkxAssetsValuationDetail
{
    [JsonPropertyName("classic")]
    public string Classic { get; init; } = string.Empty;

    [JsonPropertyName("earn")]
    public string Earn { get; init; } = string.Empty;

    [JsonPropertyName("funding")]
    public string Funding { get; init; } = string.Empty;

    [JsonPropertyName("trading")]
    public string Trading { get; init; } = string.Empty;
}
