using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Http;

public record OkxAccountBalance
{
    [JsonPropertyName("adjEq")]
    public string AdjEq { get; init; } = string.Empty;

    [JsonPropertyName("availEq")]
    public string AvailEq { get; init; } = string.Empty;

    [JsonPropertyName("borrowFroz")]
    public string BorrowFroz { get; init; } = string.Empty;

    [JsonPropertyName("imr")]
    public string Imr { get; init; } = string.Empty;

    [JsonPropertyName("isoEq")]
    public string IsoEq { get; init; } = string.Empty;

    [JsonPropertyName("mgnRatio")]
    public string MgnRatio { get; init; } = string.Empty;

    [JsonPropertyName("mmr")]
    public string Mmr { get; init; } = string.Empty;

    [JsonPropertyName("notionalUsd")]
    public string NotionalUsd { get; init; } = string.Empty;

    [JsonPropertyName("notionalUsdForBorrow")]
    public string NotionalUsdForBorrow { get; init; } = string.Empty;

    [JsonPropertyName("notionalUsdForFutures")]
    public string NotionalUsdForFutures { get; init; } = string.Empty;

    [JsonPropertyName("notionalUsdForOption")]
    public string NotionalUsdForOption { get; init; } = string.Empty;

    [JsonPropertyName("notionalUsdForSwap")]
    public string NotionalUsdForSwap { get; init; } = string.Empty;

    [JsonPropertyName("ordFroz")]
    public string OrdFroz { get; init; } = string.Empty;

    [JsonPropertyName("totalEq")]
    public string TotalEq { get; init; } = string.Empty;

    [JsonPropertyName("uTime")]
    public string UTime { get; init; } = string.Empty;

    [JsonPropertyName("upl")]
    public string Upl { get; init; } = string.Empty;

    [JsonPropertyName("details")]
    public List<OkxBalanceDetail> Details { get; init; } = new();
}
