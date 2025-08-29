using System.Threading.Channels;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx.Processors;

public class OkxMarketDataProcessor(
    [FromKeyedServices(OkxChannelNames.MarketData)] Channel<MarketData> marketDataChannel,
    ILogger<OkxMarketDataProcessor> logger,
    OkxWebSocketService okxWebSocketService) : BackgroundService
{
    private readonly Dictionary<MarketDataKey, MarketDataCache> cache = [];
    private readonly ChannelReader<MarketData> marketDataReader = marketDataChannel.Reader;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        =>

            // Disable for now. Figuring out the whole architecture atm
            // await okxWebSocketService.ConnectAsync(OkxChannelType.Public, stoppingToken);
            // await okxWebSocketService.SubscribeAsync("books", "OKB-USDT", stoppingToken);
            //
            // await ProcessMarketDataAsync(stoppingToken);
            logger.LogInformation("Markets processing is stopped");

    private async Task ProcessMarketDataAsync(CancellationToken cancellationToken)
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

    private void UpdateCache(MarketDataCache dataCache, MarketData marketData)
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
                    dataCache.Asks.Remove(price);
                }
                else
                {
                    dataCache.Asks[price] = (size, orderCount);
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
                    dataCache.Bids.Remove(price);
                }
                else
                {
                    dataCache.Bids[price] = (size, orderCount);
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
