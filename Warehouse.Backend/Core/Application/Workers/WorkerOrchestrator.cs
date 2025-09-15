using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Abstractions.Workers;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx;

namespace Warehouse.Backend.Core.Application.Workers;

public class WorkerOrchestrator(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkerOrchestrator> logger,
    IWorkerManager workerManager,
    IMarketDataCache marketDataCache) : BackgroundService
{
    private readonly TimeSpan shutdownTimeout = TimeSpan.FromSeconds(30);
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
            await StopAllWorkersAsync();
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
        foreach ((int id, WorkerInstance instance) in workerManager.GetWorkers())
        {
            if (desiredWorkers.Any(x => x.WorkerId == id))
            {
                continue;
            }

            await StopWorkerAsync(instance, id);
        }
    }

    private async Task StartWorkerAsync(WorkerConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting worker {WorkerId} for {MarketType}/{Symbol}", config.WorkerId, config.Type, config.Symbol);

            IMarketWorker worker = CreateWorker(config);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task workerTask = StartWorkerTaskAsync(worker, cts);

            var workerInstance = new WorkerInstance
            {
                Worker = worker,
                Configuration = config,
                CancellationTokenSource = cts,
                Task = workerTask,
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

    private async Task StartWorkerTaskAsync(IMarketWorker worker, CancellationTokenSource cts)
    {
        try
        {
            await worker.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker {WorkerId} was cancelled", worker.WorkerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Worker {WorkerId} encountered an error", worker.WorkerId);

            if (workerManager.GetWorker(worker.WorkerId) is { } instance)
            {
                instance.IsHealthy = false;
                instance.LastError = ex.Message;
                await workerManager.UpdateWorkerStatusAsync(worker.WorkerId, WorkerInstanceStatus.Error);
            }
        }
    }

    private IMarketWorker CreateWorker(WorkerConfiguration config)
    {
        IServiceScope scope = scopeFactory.CreateScope();
        ILogger<MarketWorker> workerLogger = scope.ServiceProvider.GetRequiredService<ILogger<MarketWorker>>();

        // todo: start managing adapters in orchestrator
        IMarketAdapter adapter = config.Type switch
        {
            MarketType.Okx => scope.ServiceProvider.GetRequiredService<OkxMarketAdapter>(),
            _ => throw new NotSupportedException($"Market type {config.Type} is not supported")
        };

        return new MarketWorker(config, marketDataCache, workerLogger);
    }

    private async Task StopAllWorkersAsync()
    {
        IReadOnlyDictionary<int, WorkerInstance> activeWorkers = workerManager.GetWorkers();
        if (activeWorkers.Count == 0)
        {
            logger.LogInformation("No workers to stop");
            return;
        }

        logger.LogInformation("Stopping {WorkerCount} workers", activeWorkers.Count);

        List<Task> stopTasks = [];
        foreach ((int id, WorkerInstance instance) in activeWorkers)
        {
            stopTasks.Add(StopWorkerAsync(instance, id));
        }

        await Task.WhenAll(stopTasks);
        logger.LogInformation("All workers stopped");
    }

    private async Task StopWorkerAsync(WorkerInstance worker, int id)
    {
        if (worker == null)
        {
            logger.LogDebug("Worker {WorkerId} not found during stop", id);
            return;
        }

        try
        {
            logger.LogInformation("Stopping worker {WorkerId}", id);

            await workerManager.UpdateWorkerStatusAsync(id, WorkerInstanceStatus.Stopping);
            await worker.CancellationTokenSource.CancelAsync();
            await worker.Worker.StopAsync();

            bool completedInTime = await WaitForTaskWithTimeoutAsync(worker.Task, shutdownTimeout);
            if (!completedInTime)
            {
                logger.LogWarning("Worker {WorkerId} did not stop within {Timeout}", id, shutdownTimeout);
            }

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

    private static async Task<bool> WaitForTaskWithTimeoutAsync(Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);
        Task completedTask = await Task.WhenAny(task, timeoutTask);
        return completedTask == task;
    }
}
