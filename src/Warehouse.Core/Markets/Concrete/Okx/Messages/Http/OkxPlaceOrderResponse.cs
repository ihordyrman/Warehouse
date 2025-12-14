using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Http;

/// <summary>
/// Individual order result from place order response
/// </summary>
public class OkxPlaceOrderResponse
{
    /// <summary>
    /// Order ID assigned by OKX
    /// </summary>
    [JsonPropertyName("ordId")]
    public string OrderId { get; init; } = string.Empty;

    /// <summary>
    /// Client order ID (echoed back)
    /// </summary>
    [JsonPropertyName("clOrdId")]
    public string ClientOrderId { get; init; } = string.Empty;

    /// <summary>
    /// Order tag (echoed back)
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; init; } = string.Empty;

    /// <summary>
    /// Individual order status code. "0" = success
    /// </summary>
    [JsonPropertyName("sCode")]
    public string StatusCode { get; init; } = string.Empty;

    /// <summary>
    /// Individual order status message (empty on success)
    /// </summary>
    [JsonPropertyName("sMsg")]
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp in milliseconds
    /// </summary>
    [JsonPropertyName("ts")]
    public string? Timestamp { get; init; }

    [JsonIgnore]
    public bool IsSuccess => StatusCode == "0";
}
