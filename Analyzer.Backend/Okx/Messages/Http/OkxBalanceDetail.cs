using System.Text.Json.Serialization;

namespace Analyzer.Backend.Okx.Messages.Http;

public record OkxBalanceDetail
{
    [JsonPropertyName("accAvgPx")]
    public string AccAvgPx { get; init; } = string.Empty;

    [JsonPropertyName("autoLendAmt")]
    public string AutoLendAmt { get; init; } = string.Empty;

    [JsonPropertyName("autoLendMtAmt")]
    public string AutoLendMtAmt { get; init; } = string.Empty;

    [JsonPropertyName("autoLendStatus")]
    public string AutoLendStatus { get; init; } = string.Empty;

    [JsonPropertyName("availBal")]
    public string AvailBal { get; init; } = string.Empty;

    [JsonPropertyName("availEq")]
    public string AvailEq { get; init; } = string.Empty;

    [JsonPropertyName("borrowFroz")]
    public string BorrowFroz { get; init; } = string.Empty;

    [JsonPropertyName("cashBal")]
    public string CashBal { get; init; } = string.Empty;

    [JsonPropertyName("ccy")]
    public string Ccy { get; init; } = string.Empty;

    [JsonPropertyName("clSpotInUseAmt")]
    public string ClSpotInUseAmt { get; init; } = string.Empty;

    [JsonPropertyName("colBorrAutoConversion")]
    public string ColBorrAutoConversion { get; init; } = string.Empty;

    [JsonPropertyName("colRes")]
    public string ColRes { get; init; } = string.Empty;

    [JsonPropertyName("collateralEnabled")]
    public bool CollateralEnabled { get; init; }

    [JsonPropertyName("collateralRestrict")]
    public bool CollateralRestrict { get; init; }

    [JsonPropertyName("crossLiab")]
    public string CrossLiab { get; init; } = string.Empty;

    [JsonPropertyName("disEq")]
    public string DisEq { get; init; } = string.Empty;

    [JsonPropertyName("eq")]
    public string Eq { get; init; } = string.Empty;

    [JsonPropertyName("eqUsd")]
    public string EqUsd { get; init; } = string.Empty;

    [JsonPropertyName("fixedBal")]
    public string FixedBal { get; init; } = string.Empty;

    [JsonPropertyName("frozenBal")]
    public string FrozenBal { get; init; } = string.Empty;

    [JsonPropertyName("imr")]
    public string Imr { get; init; } = string.Empty;

    [JsonPropertyName("interest")]
    public string Interest { get; init; } = string.Empty;

    [JsonPropertyName("isoEq")]
    public string IsoEq { get; init; } = string.Empty;

    [JsonPropertyName("isoLiab")]
    public string IsoLiab { get; init; } = string.Empty;

    [JsonPropertyName("isoUpl")]
    public string IsoUpl { get; init; } = string.Empty;

    [JsonPropertyName("liab")]
    public string Liab { get; init; } = string.Empty;

    [JsonPropertyName("maxLoan")]
    public string MaxLoan { get; init; } = string.Empty;

    [JsonPropertyName("maxSpotInUse")]
    public string MaxSpotInUse { get; init; } = string.Empty;

    [JsonPropertyName("mgnRatio")]
    public string MgnRatio { get; init; } = string.Empty;

    [JsonPropertyName("mmr")]
    public string Mmr { get; init; } = string.Empty;

    [JsonPropertyName("notionalLever")]
    public string NotionalLever { get; init; } = string.Empty;

    [JsonPropertyName("openAvgPx")]
    public string OpenAvgPx { get; init; } = string.Empty;

    [JsonPropertyName("ordFrozen")]
    public string OrdFrozen { get; init; } = string.Empty;

    [JsonPropertyName("rewardBal")]
    public string RewardBal { get; init; } = string.Empty;

    [JsonPropertyName("smtSyncEq")]
    public string SmtSyncEq { get; init; } = string.Empty;

    [JsonPropertyName("spotBal")]
    public string SpotBal { get; init; } = string.Empty;

    [JsonPropertyName("spotCopyTradingEq")]
    public string SpotCopyTradingEq { get; init; } = string.Empty;

    [JsonPropertyName("spotInUseAmt")]
    public string SpotInUseAmt { get; init; } = string.Empty;

    [JsonPropertyName("spotIsoBal")]
    public string SpotIsoBal { get; init; } = string.Empty;

    [JsonPropertyName("spotUpl")]
    public string SpotUpl { get; init; } = string.Empty;

    [JsonPropertyName("spotUplRatio")]
    public string SpotUplRatio { get; init; } = string.Empty;

    [JsonPropertyName("stgyEq")]
    public string StgyEq { get; init; } = string.Empty;

    [JsonPropertyName("totalPnl")]
    public string TotalPnl { get; init; } = string.Empty;

    [JsonPropertyName("totalPnlRatio")]
    public string TotalPnlRatio { get; init; } = string.Empty;

    [JsonPropertyName("twap")]
    public string Twap { get; init; } = string.Empty;

    [JsonPropertyName("uTime")]
    public string UTime { get; init; } = string.Empty;

    [JsonPropertyName("upl")]
    public string Upl { get; init; } = string.Empty;

    [JsonPropertyName("uplLiab")]
    public string UplLiab { get; init; } = string.Empty;
}
