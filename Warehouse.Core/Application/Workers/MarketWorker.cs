using Microsoft.Extensions.Logging;
using Warehouse.Core.Abstractions.Markets;
using Warehouse.Core.Abstractions.Workers;
using Warehouse.Core.Domain;
using Warehouse.Core.Models;

namespace Warehouse.Core.Application.Workers;

public class MarketWorker(WorkerConfiguration configuration, IMarketDataCache marketDataCache, ILogger<MarketWorker> logger) : IMarketWorker
{
    private CancellationTokenSource? cancellationTokenSource;
    private Task? processingTask;

    public int WorkerId { get; } = configuration.WorkerId;

    public MarketType MarketType { get; } = configuration.Type;

    public bool IsRunning { get; private set; }

    public WorkerState State { get; private set; } = WorkerState.Stopped;

    public DateTime? LastProcessedAt { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            logger.LogWarning("Worker {WorkerId} is already running", WorkerId);
            return;
        }

        logger.LogInformation("Starting worker {WorkerId} for {Symbol}", WorkerId, configuration.Symbol);

        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsRunning = true;
        State = WorkerState.Running;

        processingTask = ProcessMarketDataAsync(cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            logger.LogWarning("Worker {WorkerId} is not running", WorkerId);
            return;
        }

        logger.LogInformation("Stopping worker {WorkerId}", WorkerId);
        State = WorkerState.Stopped;

        await cancellationTokenSource?.CancelAsync()!;

        if (processingTask != null)
        {
            try
            {
                await processingTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Worker {WorkerId} did not stop gracefully within timeout", WorkerId);
            }
        }

        IsRunning = false;
        State = WorkerState.Stopped;
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        processingTask = null;

        logger.LogInformation("Worker {WorkerId} stopped", WorkerId);
    }

    private async Task ProcessMarketDataAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker {WorkerId} started processing loop", WorkerId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    MarketData? marketData = marketDataCache.GetData(configuration.Symbol, MarketType);

                    if (marketData != null)
                    {
                        LastProcessedAt = DateTime.UtcNow;
                    }

                    await Task.Delay(configuration.ProcessingInterval, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error processing market data in worker {WorkerId}", WorkerId);
                    State = WorkerState.Error;

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    State = WorkerState.Running;
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker {WorkerId} processing cancelled", WorkerId);
        }
        finally
        {
            logger.LogInformation("Worker {WorkerId} exited processing loop", WorkerId);
        }
    }
}
