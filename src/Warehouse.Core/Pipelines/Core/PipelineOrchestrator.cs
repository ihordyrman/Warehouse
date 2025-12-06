using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineOrchestrator
{
    Task SynchronizePipelinesAsync();

    Task<bool> StartPipelineAsync(int pipelineId);

    Task<bool> StopPipelineAsync(int pipelineId);
}

public class PipelineOrchestrator(IServiceScopeFactory scopeFactory, ILogger<PipelineOrchestrator> logger)
    : BackgroundService, IPipelineOrchestrator
{
    private readonly ConcurrentDictionary<int, IPipelineExecutor> runningExecutors = new();
    private readonly PeriodicTimer periodicTimer = new(TimeSpan.FromSeconds(30));
    private CancellationToken cancellationToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        cancellationToken = stoppingToken;
        logger.LogInformation("PipelineOrchestrator started");

        await SynchronizePipelinesAsync();

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SynchronizePipelinesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during pipeline synchronization");
            }
        }
    }

    public async Task SynchronizePipelinesAsync()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        List<Pipeline> pipelines = await dbContext.PipelineConfigurations.Include(x => x.Steps).ToListAsync(cancellationToken);

        logger.LogDebug("Synchronizing {Count} pipelines", pipelines.Count);

        foreach (Pipeline pipeline in pipelines)
        {
            bool isCurrentlyRunning = runningExecutors.ContainsKey(pipeline.Id);
            bool shouldBeRunning = pipeline.Enabled && pipeline.Status != PipelineStatus.Paused && pipeline.Status != PipelineStatus.Error;

            switch (shouldBeRunning, isCurrentlyRunning)
            {
                case (true, false):
                    await StartPipelineInternalAsync(pipeline, scope.ServiceProvider);
                    break;
                case (false, true):
                    await StopPipelineInternalAsync(pipeline.Id);
                    break;
            }
        }

        var pipelineIds = pipelines.Select(x => x.Id).ToHashSet();
        foreach (int executorId in runningExecutors.Keys)
        {
            if (!pipelineIds.Contains(executorId))
            {
                await StopPipelineInternalAsync(executorId);
            }
        }
    }

    public async Task<bool> StartPipelineAsync(int pipelineId)
    {
        if (runningExecutors.ContainsKey(pipelineId))
        {
            logger.LogWarning("Pipeline {PipelineId} is already running", pipelineId);
            return false;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        Pipeline? pipeline = await dbContext.PipelineConfigurations.Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == pipelineId, cancellationToken);

        if (pipeline is null)
        {
            logger.LogWarning("Pipeline {PipelineId} not found", pipelineId);
            return false;
        }

        return await StartPipelineInternalAsync(pipeline, scope.ServiceProvider);
    }

    public async Task<bool> StopPipelineAsync(int pipelineId) => await StopPipelineInternalAsync(pipelineId);

    private async Task<bool> StartPipelineInternalAsync(Pipeline pipeline, IServiceProvider serviceProvider)
    {
        try
        {
            var executor = new PipelineExecutor(serviceProvider, pipeline);

            if (!runningExecutors.TryAdd(pipeline.Id, executor))
            {
                logger.LogWarning("Pipeline {PipelineId} was already added by another thread", pipeline.Id);
                return false;
            }

            await executor.StartAsync(cancellationToken);
            logger.LogInformation("Started pipeline {PipelineId} ({PipelineName})", pipeline.Id, pipeline.Name);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start pipeline {PipelineId}", pipeline.Id);
            runningExecutors.TryRemove(pipeline.Id, out _);
            return false;
        }
    }

    private async Task<bool> StopPipelineInternalAsync(int pipelineId)
    {
        if (!runningExecutors.TryRemove(pipelineId, out IPipelineExecutor? executor))
        {
            logger.LogWarning("Pipeline {PipelineId} is not running", pipelineId);
            return false;
        }

        try
        {
            await executor.StopAsync();
            logger.LogInformation("Stopped pipeline {PipelineId}", pipelineId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping pipeline {PipelineId}", pipelineId);
            return false;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PipelineOrchestrator stopping, shutting down {Count} running pipelines", runningExecutors.Count);

        foreach (int pipelineId in runningExecutors.Keys.ToList())
        {
            await StopPipelineInternalAsync(pipelineId);
        }

        await base.StopAsync(stoppingToken);
    }
}
