using System.Collections.Concurrent;
using System.Collections.Frozen;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;

namespace Warehouse.Core.Markets.Services;

/// <summary>
///     Provides in-memory caching for real-time market data (order books).
/// </summary>
public interface IMarketDataCache
{
    /// <summary>
    ///     Retrieves the current market data snapshot for a specific symbol.
    /// </summary>
    MarketData? GetData(string symbol, MarketType marketType);

    /// <summary>
    ///     Updates the cache with new market data events (deltas).
    /// </summary>
    void Update(MarketDataEvent marketDataEvent);
}

/// <summary>
///     Thread-safe implementation of IMarketDataCache using concurrent dictionaries.
/// </summary>
public class MarketDataCache : IMarketDataCache
{
    private readonly ConcurrentDictionary<MarketDataKey, MarketDataSnapshot> cache = new();

    public MarketData? GetData(string symbol, MarketType marketType)
    {
        if (cache.TryGetValue(new MarketDataKey(symbol, marketType), out MarketDataSnapshot? snapshot))
        {
            return snapshot.ToMarketData();
        }

        return null;
    }

    public void Update(MarketDataEvent marketDataEvent)
    {
        MarketDataSnapshot snapshot = cache.GetOrAdd(
            new MarketDataKey(marketDataEvent.Symbol, marketDataEvent.Source),
            _ => new MarketDataSnapshot());

        foreach (string[] ask in marketDataEvent.Asks)
        {
            if (ask.Length < 4 ||
                !decimal.TryParse(ask[0], out decimal price) ||
                !decimal.TryParse(ask[1], out decimal size) ||
                !int.TryParse(ask[3], out int orderCount))
            {
                continue;
            }

            if (size == 0)
            {
                snapshot.Asks.TryRemove(price, out _);
            }
            else
            {
                snapshot.Asks[price] = (size, orderCount);
            }
        }

        foreach (string[] bid in marketDataEvent.Bids)
        {
            if (bid.Length < 4 ||
                !decimal.TryParse(bid[0], out decimal price) ||
                !decimal.TryParse(bid[1], out decimal size) ||
                !int.TryParse(bid[3], out int orderCount))
            {
                continue;
            }

            if (size == 0)
            {
                snapshot.Bids.TryRemove(price, out _);
            }
            else
            {
                snapshot.Bids[price] = (size, orderCount);
            }
        }
    }

    private record MarketDataKey(string Symbol, MarketType MarketType);

    private class MarketDataSnapshot
    {
        public ConcurrentDictionary<decimal, (decimal Size, int OrderCount)> Asks { get; } = new();

        public ConcurrentDictionary<decimal, (decimal Size, int OrderCount)> Bids { get; } = new();

        public MarketData ToMarketData() => new(Asks.ToFrozenDictionary(), Bids.ToFrozenDictionary());
    }
}
