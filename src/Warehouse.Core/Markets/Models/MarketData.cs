using System.Collections.Frozen;

namespace Warehouse.Core.Markets.Models;

public sealed record MarketData(
    FrozenDictionary<decimal, (decimal size, int count)> Asks,
    FrozenDictionary<decimal, (decimal size, int count)> Bids);
