using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

/// <summary>
///     Represents the balance of a single currency.
/// </summary>
public class Balance
{
    /// <summary>
    ///     Currency symbol (e.g., "BTC", "USDT").
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    ///     Amount available for trading or withdrawal.
    /// </summary>
    public decimal Available { get; init; }

    /// <summary>
    ///     Total amount including frozen funds.
    /// </summary>
    public decimal Total { get; init; }

    /// <summary>
    ///     Amount frozen (e.g., in open orders).
    /// </summary>
    public decimal Frozen { get; init; }

    public decimal InOrder { get; init; }

    public MarketType MarketType { get; init; }

    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
