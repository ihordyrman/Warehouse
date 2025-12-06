using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

/// <summary>
///     Aggregated balance information for a specific market account.
/// </summary>
public class AccountBalance
{
    /// <summary>
    ///     The market this balance belongs to.
    /// </summary>
    public MarketType MarketType { get; init; }

    /// <summary>
    ///     Total equity value of the account (usually in USDT).
    /// </summary>
    public decimal TotalEquity { get; init; }

    /// <summary>
    ///     Funds available for new orders.
    /// </summary>
    public decimal AvailableBalance { get; init; }

    /// <summary>
    ///     Margin currently used by open positions.
    /// </summary>
    public decimal UsedMargin { get; init; }

    /// <summary>
    ///     Unrealized Profit and Loss from open positions.
    /// </summary>
    public decimal UnrealizedPnl { get; init; }

    /// <summary>
    ///     Detailed balances for individual currencies.
    /// </summary>
    public List<Balance> Balances { get; init; } = [];

    /// <summary>
    ///     When this balance snapshot was updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
