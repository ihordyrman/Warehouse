using Microsoft.EntityFrameworkCore;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Concrete.Okx;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Services;
using Warehouse.Core.Workers.Contracts;
using Warehouse.Core.Workers.Domain;
using Warehouse.Core.Workers.Models;
using Warehouse.Core.Workers.Services;

namespace Warehouse.App;

public class WorkerOrchestrator(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkerOrchestrator> logger,
    IWorkerManager workerManager,
    IMarketDataCache marketDataCache) : BackgroundService
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly Dictionary<MarketType, MarketConnection> marketConnections = [];
    private readonly TimeSpan syncInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WorkerOrchestrator started with sync interval {Interval}", syncInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SynchronizeWorkersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during worker synchronization");
                }

                await Task.Delay(syncInterval, stoppingToken);
            }
        }
        finally
        {
            await ShutdownAsync();
            logger.LogInformation("WorkerOrchestrator stopped");
        }
    }

    private async Task SynchronizeWorkersAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        List<WorkerConfiguration> desiredWorkers = await dbContext.WorkerDetails.Where(x => x.Enabled)
            .Select(x => new WorkerConfiguration
            {
                WorkerId = x.Id,
                Type = x.Type,
                Enabled = x.Enabled,
                Symbol = x.Symbol
            })
            .ToListAsync(cancellationToken);

        foreach (IGrouping<MarketType, WorkerConfiguration> marketGroup in desiredWorkers.GroupBy(x => x.Type))
        {
            await EnsureMarketConnectionAsync(marketGroup.Key, marketGroup.ToList(), cancellationToken);
        }

        // Start missing workers
        foreach (WorkerConfiguration config in desiredWorkers)
        {
            if (workerManager.IsWorkerActive(config.WorkerId))
            {
                continue;
            }

            await StartWorkerAsync(config, cancellationToken);
        }

        // Remove redundant workers
        IReadOnlyDictionary<int, WorkerInstance> activeWorkers = workerManager.GetWorkers();
        foreach ((int id, WorkerInstance instance) in activeWorkers)
        {
            if (desiredWorkers.All(x => x.WorkerId != id))
            {
                await StopWorkerAsync(instance, id);
            }
        }

        await DisconnectUnusedMarketsAsync(desiredWorkers);
    }

    private async Task StartWorkerAsync(WorkerConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting worker {WorkerId} for {MarketType}/{Symbol}", config.WorkerId, config.Type, config.Symbol);

            IMarketWorker worker = CreateWorker(config);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task task = worker.StartAsync(cts.Token);

            var workerInstance = new WorkerInstance
            {
                Worker = worker,
                Configuration = config,
                CancellationTokenSource = cts,
                Task = task,
                StartedAt = DateTime.UtcNow,
                Status = WorkerInstanceStatus.Starting,
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow,
                LastStatusUpdate = DateTime.UtcNow
            };

            await workerManager.AddWorkerAsync(config.WorkerId, workerInstance);
            await workerManager.UpdateWorkerStatusAsync(config.WorkerId, WorkerInstanceStatus.Running);

            logger.LogInformation("Successfully started worker {WorkerId}", config.WorkerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start worker {WorkerId}", config.WorkerId);
        }
    }

    private async Task EnsureMarketConnectionAsync(
        MarketType marketType,
        List<WorkerConfiguration> workers,
        CancellationToken cancellationToken)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (marketConnections.TryGetValue(marketType, out MarketConnection? connection) && connection.IsConnected)
            {
                await UpdateSubscriptionsAsync(connection, workers, cancellationToken);
                return;
            }

            logger.LogInformation("Establishing connection to {MarketType}", marketType);

            IMarketAdapter adapter = CreateMarketAdapter(marketType);
            await adapter.ConnectAsync(cancellationToken);

            var symbols = workers.Select(x => x.Symbol).Distinct().ToList();
            foreach (string symbol in symbols)
            {
                await adapter.SubscribeAsync(symbol, cancellationToken);
            }

            marketConnections[marketType] = new MarketConnection
            {
                MarketType = marketType,
                Adapter = adapter,
                Symbols = symbols.ToHashSet(),
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow
            };

            logger.LogInformation("Successfully connected to {MarketType} with {SymbolCount} symbols", marketType, symbols.Count);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private async Task UpdateSubscriptionsAsync(
        MarketConnection connection,
        List<WorkerConfiguration> workers,
        CancellationToken cancellationToken)
    {
        var requiredSymbols = workers.Select(x => x.Symbol).Distinct().ToHashSet();

        IEnumerable<string> newSymbols = requiredSymbols.Except(connection.Symbols);
        foreach (string symbol in newSymbols)
        {
            logger.LogInformation("Adding subscription for {Symbol} on {MarketType}", symbol, connection.MarketType);
            await connection.Adapter.SubscribeAsync(symbol, cancellationToken);
            connection.Symbols.Add(symbol);
        }

        IEnumerable<string> unusedSymbols = connection.Symbols.Except(requiredSymbols);
        foreach (string symbol in unusedSymbols)
        {
            logger.LogInformation("Removing subscription for {Symbol} on {MarketType}", symbol, connection.MarketType);
            await connection.Adapter.UnsubscribeAsync(symbol, cancellationToken);
            connection.Symbols.Remove(symbol);
        }
    }

    private async Task DisconnectUnusedMarketsAsync(List<WorkerConfiguration> activeWorkers)
    {
        var activeMarkets = activeWorkers.Select(x => x.Type).Distinct().ToHashSet();
        var marketsToDisconnect = marketConnections.Keys.Where(x => !activeMarkets.Contains(x)).ToList();

        foreach (MarketType marketType in marketsToDisconnect)
        {
            await DisconnectMarketAsync(marketType);
        }
    }

    private async Task DisconnectMarketAsync(MarketType marketType)
    {
        await connectionLock.WaitAsync();
        try
        {
            if (!marketConnections.TryGetValue(marketType, out MarketConnection? connection))
            {
                return;
            }

            logger.LogInformation("Disconnecting from {MarketType}", marketType);

            try
            {
                await connection.Adapter.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disconnecting from {MarketType}", marketType);
            }

            marketConnections.Remove(marketType);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private IMarketWorker CreateWorker(WorkerConfiguration config)
    {
        IServiceScope scope = scopeFactory.CreateScope();
        ILogger<MarketWorker> workerLogger = scope.ServiceProvider.GetRequiredService<ILogger<MarketWorker>>();

        return new MarketWorker(config, marketDataCache, workerLogger);
    }

    private IMarketAdapter CreateMarketAdapter(MarketType marketType)
    {
        IServiceScope scope = scopeFactory.CreateScope();

        return marketType switch
        {
            MarketType.Okx => scope.ServiceProvider.GetRequiredService<OkxMarketAdapter>(),
            _ => throw new NotSupportedException($"Market type {marketType} is not supported")
        };
    }

    private async Task ShutdownAsync()
    {
        logger.LogInformation("Shutting down WorkerOrchestrator");

        IReadOnlyDictionary<int, WorkerInstance> activeWorkers = workerManager.GetWorkers();
        IEnumerable<Task> stopTasks = activeWorkers.Select(x => StopWorkerAsync(x.Value, x.Key));
        await Task.WhenAll(stopTasks);

        await connectionLock.WaitAsync();
        try
        {
            IEnumerable<Task> disconnectTasks = marketConnections.Values.Select(async connection =>
            {
                try
                {
                    await connection.Adapter.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disconnecting from {MarketType}", connection.MarketType);
                }
            });

            await Task.WhenAll(disconnectTasks);
            marketConnections.Clear();
        }
        finally
        {
            connectionLock.Release();
        }

        logger.LogInformation("WorkerOrchestrator shutdown complete");
    }

    private async Task StopWorkerAsync(WorkerInstance worker, int id)
    {
        if (worker is null)
        {
            logger.LogWarning("Worker {WorkerId} not found during stop", id);
            return;
        }

        try
        {
            logger.LogInformation("Stopping worker {WorkerId}", id);
            await workerManager.UpdateWorkerStatusAsync(id, WorkerInstanceStatus.Stopping);
            await worker.Worker.StopAsync();

            await workerManager.UpdateWorkerStatusAsync(id, WorkerInstanceStatus.Stopped);
            await workerManager.RemoveWorkerAsync(id);

            logger.LogInformation("Successfully stopped worker {WorkerId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping worker {WorkerId}", id);
        }
        finally
        {
            worker.CancellationTokenSource.Dispose();
        }
    }

    // maybe implement MarketManager instead? but definitely not today
    private class MarketConnection
    {
        public MarketType MarketType { get; init; }

        public IMarketAdapter Adapter { get; init; } = null!;

        public HashSet<string> Symbols { get; init; } = [];

        public bool IsConnected { get; init; }

        public DateTime ConnectedAt { get; set; }
    }
}
