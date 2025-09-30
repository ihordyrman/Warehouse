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
    private readonly PeriodicTimer periodicTimer = new(TimeSpan.FromSeconds(10));
    private static readonly DateTime SyncStartDate = DateTime.Today;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();
        Candlestick? latestCandle = await candlestickService.GetLatestCandlestickAsync(Symbol, MarketType.Okx, Timeframe, stoppingToken);

        DateTime startFrom = latestCandle?.Timestamp.AddMinutes(-2) ?? SyncStartDate;

        await SyncCandlesticksAsync(startFrom, stoppingToken);
    }

    private async Task SyncCandlesticksAsync(DateTime fromDate, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting candlestick sync for {Symbol} from {FromDate}", Symbol, fromDate);

        int totalSaved = 0;
        DateTime? lastSynced = null;

        while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                OkxHttpService okxHttpService = scope.ServiceProvider.GetRequiredService<OkxHttpService>();
                ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();

                string before = lastSynced.HasValue ?
                    new DateTimeOffset(lastSynced.Value).ToUnixTimeMilliseconds().ToString() :
                    new DateTimeOffset(fromDate).ToUnixTimeMilliseconds().ToString();

                Result<OkxCandlestick[]> result = await okxHttpService.GetCandlesticksAsync(
                    Symbol,
                    Timeframe,
                    before: before,
                    limit: BatchSize);

                if (!result.IsSuccess)
                {
                    logger.LogError("Failed to fetch candlesticks: {Error}", result.Error);
                    continue;
                }

                OkxCandlestick[]? okxCandles = result.Value;

                if (okxCandles == null || okxCandles.Length == 0)
                {
                    logger.LogInformation("No more candlesticks to fetch");
                    continue;
                }

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

                int saved = await candlestickService.SaveCandlesticksAsync(candlesticks, cancellationToken);
                totalSaved += saved;

                logger.LogInformation(
                    "Saved batch of {Count} candlesticks. Latest: {LatestTimestamp}. Total saved: {TotalSaved}",
                    saved,
                    candlesticks.Max(x => x.Timestamp),
                    totalSaved);

                if (okxCandles.Length < BatchSize)
                {
                    continue;
                }

                DateTime oldestInBatch = okxCandles.Min(x => x.Timestamp);
                lastSynced = oldestInBatch;
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

        logger.LogInformation("Sync completed for {Symbol}. Total saved: {TotalSaved}", Symbol, totalSaved);
    }
}
