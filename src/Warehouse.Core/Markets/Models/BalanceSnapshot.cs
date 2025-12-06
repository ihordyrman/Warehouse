using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Models;

/// <summary>
///     Snapshot of all balances for a market at a specific point in time.
/// </summary>
public class BalanceSnapshot
{
    public MarketType MarketType { get; init; }

    /// <summary>
    ///     Spot account balances by currency.
    /// </summary>
    public Dictionary<string, Balance> Spot { get; } = [];

    /// <summary>
    ///     Funding account balances by currency.
    /// </summary>
    public Dictionary<string, Balance> Funding { get; } = [];

    /// <summary>
    ///     Overall account summary (equity, margin, etc.).
    /// </summary>
    public AccountBalance? AccountSummary { get; set; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
