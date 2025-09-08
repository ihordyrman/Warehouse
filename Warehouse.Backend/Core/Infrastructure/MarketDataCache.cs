using System.Collections.Concurrent;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Infrastructure;

public class MarketDataCache(ILogger<MarketDataCache> logger) : IMarketDataCache
{
    private readonly ConcurrentDictionary<string, MarketData> cache = new();

    public MarketData? GetData(string symbol)
    {
        cache.TryGetValue(symbol, out MarketData? data);
        return data;
    }

    public void Update(string symbol, MarketData data)
    {
        cache.AddOrUpdate(symbol, data, (_, _) => data);
        logger.LogDebug("Updated market data for {Symbol}: Bid={Bid}, Ask={Ask}", symbol, data.BidPrice, data.AskPrice);
    }

    public IReadOnlySet<string> GetCachedSymbols() => cache.Keys.ToHashSet();
}
