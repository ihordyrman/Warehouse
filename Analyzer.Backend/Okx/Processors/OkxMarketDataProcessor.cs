using System.Threading.Channels;
using Analyzer.Backend.Okx.Models;

namespace Analyzer.Backend.Okx.Processors;

public class OkxMarketDataProcessor(
    [FromKeyedServices(OkxChannelNames.MarketData)] Channel<MarketData> marketDataChannel,
    ILogger<OkxMarketDataProcessor> logger)
{
    private readonly ChannelReader<MarketData> marketDataReader = marketDataChannel.Reader;
    private readonly Dictionary<MarketDataKey, MarketDataCache> cache = [];

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        await foreach (MarketData marketData in marketDataReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                if (TryGetCache(marketData.Channel, marketData.Instrument, out MarketDataCache? dataCache))
                {
                    UpdateCache(dataCache!, marketData);
                }
                else
                {
                    var newCache = new MarketDataCache(marketData.Channel, marketData.Instrument);
                    UpdateCache(newCache, marketData);
                    AddCache(newCache);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing market data for {Channel}:{Instrument}", marketData.Channel, marketData.Instrument);
            }
        }
    }

    private void UpdateCache(MarketDataCache cache, MarketData marketData)
    {
        foreach (string[] ask in marketData.Asks)
        {
            if (ask.Length >= 4 &&
                decimal.TryParse(ask[0], out decimal price) &&
                decimal.TryParse(ask[1], out decimal size) &&
                int.TryParse(ask[3], out int orderCount))
            {
                if (size == 0)
                {
                    cache.Asks.Remove(price);
                }
                else
                {
                    cache.Asks[price] = (size, orderCount);
                }
            }
        }

        foreach (string[] bid in marketData.Bids)
        {
            if (bid.Length >= 4 &&
                decimal.TryParse(bid[0], out decimal price) &&
                decimal.TryParse(bid[1], out decimal size) &&
                int.TryParse(bid[3], out int orderCount))
            {
                if (size == 0)
                {
                    cache.Bids.Remove(price);
                }
                else
                {
                    cache.Bids[price] = (size, orderCount);
                }
            }
        }
    }

    private bool TryGetCache(string channel, string instrument, out MarketDataCache? marketCache)
    {
        var key = new MarketDataKey(channel, instrument);
        return cache.TryGetValue(key, out marketCache);
    }

    private void AddCache(MarketDataCache marketData) => cache[marketData.Key] = marketData;
}
