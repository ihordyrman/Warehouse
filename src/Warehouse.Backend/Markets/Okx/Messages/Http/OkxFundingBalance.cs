using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Http;

public record OkxFundingBalance
{
    [JsonPropertyName("availBal")]
    public string AvailBal { get; init; } = string.Empty;

    [JsonPropertyName("bal")]
    public string Bal { get; init; } = string.Empty;

    [JsonPropertyName("ccy")]
    public string Ccy { get; init; } = string.Empty;

    [JsonPropertyName("frozenBal")]
    public string FrozenBal { get; init; } = string.Empty;
}
