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

    private void UpdateCache(MarketData snapshot, MarketData marketData)
    {
        // foreach (string[] ask in marketData.Asks)
        // {
        //     if (ask.Length >= 4 &&
        //         decimal.TryParse(ask[0], out decimal price) &&
        //         decimal.TryParse(ask[1], out decimal size) &&
        //         int.TryParse(ask[3], out int orderCount))
        //     {
        //         if (size == 0)
        //         {
        //             snapshot.Asks.Remove(price);
        //         }
        //         else
        //         {
        //             snapshot.Asks[price] = (size, orderCount);
        //         }
        //     }
        // }
        //
        // foreach (string[] bid in marketData.Bids)
        // {
        //     if (bid.Length >= 4 &&
        //         decimal.TryParse(bid[0], out decimal price) &&
        //         decimal.TryParse(bid[1], out decimal size) &&
        //         int.TryParse(bid[3], out int orderCount))
        //     {
        //         if (size == 0)
        //         {
        //             snapshot.Bids.Remove(price);
        //         }
        //         else
        //         {
        //             snapshot.Bids[price] = (size, orderCount);
        //         }
        //     }
        // }
    }

    public IReadOnlySet<string> GetCachedSymbols() => cache.Keys.ToHashSet();
}
