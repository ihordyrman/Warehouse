using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Markets.Concrete.Okx.Constants;
using Warehouse.Core.Markets.Concrete.Okx.Messages.Http;
using Warehouse.Core.Markets.Concrete.Okx.Services;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared;
using Warehouse.Core.Shared.Domain;
using Warehouse.Core.Shared.Services;
using static Warehouse.Core.Shared.Domain.Instrument;

namespace Warehouse.Core.Markets.Concrete.Okx;

public class OkxSynchronizationWorker(IServiceScopeFactory scopeFactory, ILogger<OkxSynchronizationWorker> logger) : BackgroundService
{
    private const string Timeframe = CandlestickTimeframes.OneMinute;
    private const int BatchSize = 100;
    private static readonly Instrument[] Symbols = [OKB, BTC, SOL, ETH, DOGE, XRP, BCH, LTC];
    private static readonly DateTime SyncStartDate = DateTime.Today;
    private readonly PeriodicTimer periodicTimer = new(TimeSpan.FromMinutes(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Candlestick> candlesticks = [];
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();
            OkxHttpService okxHttpService = scope.ServiceProvider.GetRequiredService<OkxHttpService>();

            foreach (Instrument symbol in Symbols)
            {
                var pairSymbol = new Pair(symbol, USDT);
                Candlestick? latestCandle = await candlestickService.GetLatestCandlestickAsync(
                    pairSymbol,
                    MarketType.Okx,
                    Timeframe,
                    stoppingToken);
                DateTime startFrom = latestCandle?.Timestamp.AddMinutes(-2) ?? SyncStartDate;

                await foreach (Candlestick candlestick in GetCandlesticksAsync(pairSymbol, startFrom, okxHttpService)
                                   .WithCancellation(stoppingToken))
                {
                    candlesticks.Add(candlestick);
                }
            }

            int saved = await candlestickService.SaveCandlesticksAsync(candlesticks, stoppingToken);

            logger.LogInformation(
                "Saved batch of {Count} candlesticks. Latest: {LatestTimestamp}.",
                saved,
                candlesticks.Max(x => x.Timestamp));

            candlesticks.Clear();
        }
    }

    private async IAsyncEnumerable<Candlestick> GetCandlesticksAsync(string symbol, DateTime fromDate, OkxHttpService okxHttpService)
    {
        logger.LogInformation("Starting candlestick sync for {Symbol} from {FromDate}", symbol, fromDate);

        var before = new DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString();

        Result<OkxCandlestick[]> result = await okxHttpService.GetCandlesticksAsync(symbol, Timeframe, before: before, limit: BatchSize);

        if (!result.IsSuccess)
        {
            logger.LogError("Failed to fetch candlesticks: {Error}", result.Error);
            yield break;
        }

        OkxCandlestick[]? okxCandles = result.Value;

        if (okxCandles is null || okxCandles.Length == 0)
        {
            logger.LogInformation("No more candlesticks to fetch");
            yield break;
        }

        logger.LogDebug("Fetched {Count} candlesticks", okxCandles.Length);

        foreach (OkxCandlestick okxCandle in okxCandles)
        {
            yield return new Candlestick
            {
                Symbol = symbol,
                MarketType = MarketType.Okx,
                Timestamp = okxCandle.Timestamp,
                Open = okxCandle.Open,
                High = okxCandle.High,
                Low = okxCandle.Low,
                Close = okxCandle.Close,
                Volume = okxCandle.Volume,
                VolumeQuote = okxCandle.VolumeQuoteCurrency,
                IsCompleted = okxCandle.IsCompleted,
                Timeframe = Timeframe
            };
        }
    }
}
