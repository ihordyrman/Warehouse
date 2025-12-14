using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

/// <summary>
/// Request body for placing an order on OKX.
/// POST /api/v5/trade/order
/// </summary>
public class OkxPlaceOrderRequest
{
    /// <summary>
    /// Instrument ID, e.g. "BTC-USDT"
    /// </summary>
    [JsonPropertyName("instId")]
    public required string InstrumentId { get; init; }

    /// <summary>
    /// Trade mode: "cash" (spot), "cross" (cross margin), "isolated" (isolated margin)
    /// </summary>
    [JsonPropertyName("tdMode")]
    public required string TradeMode { get; init; }

    /// <summary>
    /// Order side: "buy" or "sell"
    /// </summary>
    [JsonPropertyName("side")]
    public required string Side { get; init; }

    /// <summary>
    /// Order type: "market", "limit", "post_only", "fok", "ioc"
    /// </summary>
    [JsonPropertyName("ordType")]
    public required string OrderType { get; init; }

    /// <summary>
    /// Quantity to buy or sell
    /// </summary>
    [JsonPropertyName("sz")]
    public required string Size { get; init; }

    /// <summary>
    /// Price (required for limit orders, omit for market orders)
    /// </summary>
    [JsonPropertyName("px")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Price { get; init; }

    /// <summary>
    /// Client-supplied order ID (max 32 chars)
    /// </summary>
    [JsonPropertyName("clOrdId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientOrderId { get; init; }

    /// <summary>
    /// Order tag (max 16 chars, used for broker identification)
    /// </summary>
    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }

    /// <summary>
    /// Whether to reduce position only. Only applicable to margin/futures.
    /// </summary>
    [JsonPropertyName("reduceOnly")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ReduceOnly { get; init; }

    /// <summary>
    /// Target currency for market buy orders.
    /// "base_ccy" = quantity in base currency
    /// "quote_ccy" = quantity in quote currency (e.g., spend $100 worth)
    /// </summary>
    [JsonPropertyName("tgtCcy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetCurrency { get; init; }
}
