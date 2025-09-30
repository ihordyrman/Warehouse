using Warehouse.Backend.Core;
using Warehouse.Backend.Core.Application.Services;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Messages.Http;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx;

public class OkxSynchronizationWorker(IServiceScopeFactory scopeFactory, ILogger<OkxSynchronizationWorker> logger) : BackgroundService
{
    private const string Symbol = "OKB-USDT";
    private const string Timeframe = CandlestickTimeframes.OneMinute;
    private const int BatchSize = 100;

    private static readonly DateTime SyncStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();
        Candlestick? latestCandle = await candlestickService.GetLatestCandlestickAsync(Symbol, MarketType.Okx, Timeframe, stoppingToken);

        DateTime startFrom = latestCandle?.Timestamp ?? SyncStartDate;

        if (latestCandle != null)
        {
            logger.LogInformation("Found existing data. Latest candlestick: {Timestamp}. Syncing from there.", latestCandle.Timestamp);
        }
        else
        {
            logger.LogInformation("No existing data found. Starting full sync from {StartDate}", SyncStartDate);
        }

        await SyncCandlesticksAsync(startFrom, DateTime.UtcNow, stoppingToken);
    }

    private async Task SyncCandlesticksAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        OkxHttpService okxHttpService = scope.ServiceProvider.GetRequiredService<OkxHttpService>();
        ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();

        logger.LogInformation("Starting candlestick sync for {Symbol} from {FromDate} to {ToDate}", Symbol, fromDate, toDate);

        int totalFetched = 0;
        int totalSaved = 0;
        DateTime? currentAfter = null;
        bool hasMoreData = true;

        while (hasMoreData && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                string? afterTimestamp = currentAfter.HasValue ?
                    new DateTimeOffset(currentAfter.Value).ToUnixTimeMilliseconds().ToString() :
                    new DateTimeOffset(SyncStartDate).ToUnixTimeMilliseconds().ToString();

                string beforeTimestamp = new DateTimeOffset(toDate).ToUnixTimeMilliseconds().ToString();

                Result<OkxCandlestick[]> result = await okxHttpService.GetCandlesticksAsync(
                    Symbol,
                    Timeframe,
                    afterTimestamp,
                    beforeTimestamp,
                    BatchSize);

                if (!result.IsSuccess)
                {
                    logger.LogError("Failed to fetch candlesticks: {Error}", result.Error);
                    break;
                }

                OkxCandlestick[]? okxCandles = result.Value;

                if (okxCandles == null || okxCandles.Length == 0)
                {
                    logger.LogInformation("No more candlesticks to fetch");
                    hasMoreData = false;
                    break;
                }

                totalFetched += okxCandles.Length;
                logger.LogDebug("Fetched {Count} candlesticks", okxCandles.Length);

                var candlesticks = okxCandles.Select(okxCandle => new Candlestick
                    {
                        Symbol = Symbol,
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
                    })
                    .ToList();

                await candlestickService.SaveCandlesticksAsync(candlesticks, cancellationToken);
                totalSaved += candlesticks.Count;

                logger.LogInformation(
                    "Saved batch of {Count} candlesticks. Latest: {LatestTimestamp}. Total fetched: {TotalFetched}, Total saved: {TotalSaved}",
                    candlesticks.Count,
                    candlesticks.Max(c => c.Timestamp),
                    totalFetched,
                    totalSaved);

                if (okxCandles.Length < BatchSize)
                {
                    hasMoreData = false;
                    break;
                }

                DateTime oldestInBatch = okxCandles.Min(c => c.Timestamp);
                currentAfter = oldestInBatch;

                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Sync operation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during batch fetch/save");
            }
        }

        logger.LogInformation(
            "Sync completed for {Symbol}. Total fetched: {TotalFetched}, Total saved: {TotalSaved}",
            Symbol,
            totalFetched,
            totalSaved);
    }
}
