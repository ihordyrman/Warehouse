using System.Collections.Frozen;

namespace Warehouse.Core.Markets.Models;

/// <summary>
///     Immutable snapshot of the order book (asks and bids).
/// </summary>
public sealed record MarketData(
    FrozenDictionary<decimal, (decimal size, int count)> Asks,
    FrozenDictionary<decimal, (decimal size, int count)> Bids);
