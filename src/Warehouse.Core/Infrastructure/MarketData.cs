using System.Collections.Frozen;
using Warehouse.Core.Domain;

namespace Warehouse.Core.Infrastructure;

public sealed record MarketData(
    FrozenDictionary<decimal, (decimal size, int count)> Asks,
    FrozenDictionary<decimal, (decimal size, int count)> Bids);

public sealed record MarketDataEvent(string Symbol, MarketType Source, long SequenceNumber, string[][] Asks, string[][] Bids);
