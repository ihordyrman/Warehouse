using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Concrete.Okx.Constants;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Markets.Services;
using Warehouse.Core.Pipelines.Builder;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;
using Warehouse.Core.Shared.Domain;
using Warehouse.Core.Shared.Services;

namespace Warehouse.Core.Pipelines.Core;

/// <summary>
///     Responsible for running a single pipeline configuration loop.
///     Instantiates and executes steps sequentially.
/// </summary>
public interface IPipelineExecutor
{
    int PipelineId { get; }

    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct);

    Task StopAsync();
}

/// <summary>
///     Standard implementation of IPipelineExecutor.
///     Runs steps in a continuous loop until stopped.
/// </summary>
public class PipelineExecutor(IServiceProvider serviceProvider, Pipeline configuration) : IPipelineExecutor
{
    private CancellationTokenSource? cts;
    private Task? executionTask;

    public int PipelineId { get; } = configuration.Id;

    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken ct)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        executionTask = Task.Run(() => ExecuteLoopAsync(cts.Token), ct);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning || cts is null)
        {
            return;
        }

        await cts.CancelAsync();

        if (executionTask is not null)
        {
            try
            {
                await executionTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        IsRunning = false;
        cts.Dispose();
        cts = null;
    }

    private async Task ExecuteLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
                ILogger<PipelineExecutor> logger = scope.ServiceProvider.GetRequiredService<ILogger<PipelineExecutor>>();
                IPipelineBuilder pipelineBuilder = scope.ServiceProvider.GetRequiredService<IPipelineBuilder>();
                WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
                Pipeline pipeline = await dbContext.PipelineConfigurations.Include(x => x.Steps).FirstAsync(x => x.Id == PipelineId, ct);

                // already sorted in builder
                IReadOnlyList<IPipelineStep<TradingContext>> steps = pipelineBuilder.BuildSteps(pipeline, scope.ServiceProvider);

                if (steps.Count == 0)
                {
                    logger.LogWarning("Pipeline {PipelineId} has no enabled steps, skipping execution", PipelineId);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();
                Candlestick? recentCandlestick = await candlestickService.GetLatestCandlestickAsync(
                    pipeline.Symbol,
                    pipeline.MarketType,
                    CandlestickTimeframes.OneMinute,
                    ct);

                if (recentCandlestick is null)
                {
                    logger.LogWarning(
                        "No recent candlestick data for {Symbol} {MarketType}, skipping execution",
                        pipeline.Symbol,
                        pipeline.MarketType);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                IMarketDataCache marketDataCache = scope.ServiceProvider.GetRequiredService<IMarketDataCache>();
                MarketData? currentMarketData = marketDataCache.GetData(pipeline.Symbol, pipeline.MarketType);

                if (currentMarketData is null)
                {
                    logger.LogWarning(
                        "No market data in cache for {Symbol} {MarketType}, skipping execution",
                        pipeline.Symbol,
                        pipeline.MarketType);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                var context = new TradingContext
                {
                    PipelineId = PipelineId,
                    Symbol = pipeline.Symbol,
                    MarketType = pipeline.MarketType,
                    CurrentMarketData = marketDataCache.GetData(pipeline.Symbol, pipeline.MarketType),
                    CurrentPrice = recentCandlestick.Close
                };

                foreach (IPipelineStep<TradingContext> step in steps)
                {
                    logger.LogDebug("Executing step {StepName} for pipeline {PipelineId}", step.Name, PipelineId);

                    try
                    {
                        PipelineStepResult result = await step.ExecuteAsync(context, ct);
                        logger.LogDebug(
                            "Step {StepName} result: {Message} (Continue: {Continue})",
                            step.Name,
                            result.Message,
                            result.ShouldContinue);

                        if (!result.ShouldContinue)
                        {
                            logger.LogDebug("Pipeline {PipelineId} stopped at step {StepName}", PipelineId, step.Name);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error executing step {StepName} for pipeline {PipelineId}", step.Name, PipelineId);
                        break;
                    }
                }

                await Task.Delay(pipeline.ExecutionInterval, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsRunning = false;
        }
    }
}
