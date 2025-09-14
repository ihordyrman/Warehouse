using System.Collections.Frozen;
using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Models;

public sealed record MarketData(
    FrozenDictionary<decimal, (decimal size, int count)> Asks,
    FrozenDictionary<decimal, (decimal size, int count)> Bids);

public sealed record MarketDataEvent(string Symbol, MarketType Source, long SequenceNumber, string[][] Asks, string[][] Bids);
