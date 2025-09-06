using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Application.Workers;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx;

namespace Warehouse.Backend.Core.Abstractions.Workers;

public class WorkerOrchestrator(IServiceScopeFactory scopeFactory, ILogger<WorkerOrchestrator> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, WorkerInstance> activeWorkers = new();
    private readonly TimeSpan checkInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WorkerOrchestrator started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckWorkersHealthAsync();
                await SynchronizeWorkersAsync(stoppingToken);
                await Task.Delay(checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in WorkerOrchestrator main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        await StopAllWorkersAsync();
        logger.LogInformation("WorkerOrchestrator stopped");
    }

    private async Task SynchronizeWorkersAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        List<WorkerConfiguration> workers = await dbContext.WorkerDetails.Select(x => new WorkerConfiguration
            {
                WorkerId = x.Id,
                Type = x.Type,
                Enabled = x.Enabled,
                Symbol = x.Symbol
            })
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (WorkerConfiguration worker in workers)
        {
            if (worker.Enabled && activeWorkers.ContainsKey(worker.WorkerId))
            {
                continue;
            }

            await StartWorkerAsync(worker, scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task StartWorkerAsync(WorkerConfiguration config, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting worker {WorkerId} for {MarketType}/{Symbol}", config.WorkerId, config.Type, config.Symbol);
            IMarketWorker worker = CreateWorker(config.Type, serviceProvider, config);
            if (worker == null)
            {
                logger.LogError("Failed to create worker for market type {MarketType}", config.Type);
                return;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var workerTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await worker.StartTradingAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Worker {WorkerId} was cancelled", config.WorkerId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Worker {WorkerId} crashed", config.WorkerId);
                        if (activeWorkers.TryGetValue(config.WorkerId, out WorkerInstance? instance))
                        {
                            instance.IsHealthy = false;
                            instance.LastError = ex.Message;
                        }
                    }
                },
                cts.Token);

            var workerInstance = new WorkerInstance
            {
                Worker = worker,
                Configuration = config,
                CancellationTokenSource = cts,
                Task = workerTask,
                StartedAt = DateTime.UtcNow,
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow
            };

            if (activeWorkers.TryAdd(config.WorkerId, workerInstance))
            {
                logger.LogInformation("Successfully started worker {WorkerId}", config.WorkerId);
            }
            else
            {
                await cts.CancelAsync();
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start worker {WorkerId}", config.WorkerId);
        }
    }

    private static IMarketWorker CreateWorker(MarketType marketType, IServiceProvider serviceProvider, WorkerConfiguration config)
    {
        OkxMarketAdapter adapter = marketType switch
        {
            MarketType.Okx => serviceProvider.GetService<OkxMarketAdapter>()!,
            _ => throw new ArgumentOutOfRangeException()
        };

        return new MarketWorker(adapter, config);
    }

    private void CheckWorkersHealthAsync()
    {
        foreach ((int id, WorkerInstance instance) in activeWorkers)
        {
            try
            {
                if (instance.Task.IsCompleted)
                {
                    instance.IsHealthy = false;
                    if (instance.Task.IsFaulted)
                    {
                        instance.LastError = instance.Task.Exception?.GetBaseException().Message;
                    }

                    logger.LogWarning(
                        "Worker {WorkerId} (Type: {MarketType}, Symbol: {Symbol}) completed unexpectedly without error",
                        instance.Worker.WorkerId,
                        instance.Worker.MarketType,
                        instance.Configuration.Symbol);
                }
                else
                {
                    instance.IsHealthy = instance.Worker.IsConnected;
                    if (instance.IsHealthy)
                    {
                        logger.LogWarning(
                            "Worker {WorkerId} (Type: {MarketType}, Symbol: {Symbol}) lost connection",
                            instance.Worker.WorkerId,
                            instance.Worker.MarketType,
                            instance.Configuration.Symbol);
                    }
                }

                instance.LastHealthCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking health for worker {WorkerId}", instance.Worker.WorkerId);
            }
        }
    }

    private async Task StopAllWorkersAsync()
    {
        logger.LogInformation("Stopping all workers");

        var stopTasks = activeWorkers.Keys.Select(StopWorkerAsync).ToList();
        await Task.WhenAll(stopTasks);

        logger.LogInformation("All workers stopped");
    }

    private async Task StopWorkerAsync(int workerId)
    {
        if (activeWorkers.Remove(workerId, out WorkerInstance? instance))
        {
            try
            {
                logger.LogInformation("Stopping worker {WorkerId}", instance.Worker.WorkerId);

                await instance.CancellationTokenSource.CancelAsync();
                await instance.Worker.StopTradingAsync();

                if (!await WaitForTaskCompletionAsync(instance.Task, TimeSpan.FromSeconds(30)))
                {
                    logger.LogWarning("Worker {WorkerId} did not stop within timeout", instance.Worker.WorkerId);
                }

                instance.CancellationTokenSource.Dispose();
                logger.LogInformation("Successfully stopped worker {WorkerId}", instance.Worker.WorkerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping worker {WorkerId}", instance.Worker.WorkerId);
            }
        }
    }

    private static async Task<bool> WaitForTaskCompletionAsync(Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        Task completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        return completedTask == task;
    }

    private class WorkerInstance
    {
        public required IMarketWorker Worker { get; init; }

        public required WorkerConfiguration Configuration { get; init; }

        public required CancellationTokenSource CancellationTokenSource { get; init; }

        public required Task Task { get; init; }

        public DateTime StartedAt { get; init; }

        public DateTime LastHealthCheck { get; set; }

        public bool IsHealthy { get; set; }

        public string? LastError { get; set; }
    }
}
