using System.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Functional.Shared.Domain;
using Warehouse.Core.Old.Markets.Concrete.Okx.Constants;
using Warehouse.Core.Old.Pipelines.Builder;
using Warehouse.Core.Old.Pipelines.Trading;

namespace Warehouse.Core.Old.Pipelines.Core;

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
                IDbConnection db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
                var pipeline = await db.QueryFirstAsync<Pipeline>(
                    "SELECT * FROM pipeline_configurations WHERE id = @Id",
                    new { Id = PipelineId });

                // already sorted in builder
                IReadOnlyList<IPipelineStep<TradingContext>> steps = pipelineBuilder.BuildSteps(pipeline, scope.ServiceProvider);

                if (steps.Count == 0)
                {
                    logger.LogWarning("Pipeline {PipelineId} has no enabled steps, skipping execution", PipelineId);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                ICandlestickService candlestickService = scope.ServiceProvider.GetRequiredService<ICandlestickService>();
                FSharpOption<Candlestick> recentCandlestick = await candlestickService.GetLatestCandlestickAsync(
                    pipeline.Symbol,
                    pipeline.MarketType,
                    CandlestickTimeframes.OneMinute,
                    ct);

                if (recentCandlestick.Value is null)
                {
                    logger.LogWarning(
                        "No recent candlestick data for {Symbol} {MarketType}, skipping execution",
                        pipeline.Symbol,
                        pipeline.MarketType);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                IMarketDataCache marketDataCache = scope.ServiceProvider.GetRequiredService<IMarketDataCache>();
                FSharpOption<MarketData> currentMarketData = marketDataCache.GetData(pipeline.Symbol, pipeline.MarketType);

                if (currentMarketData is null)
                {
                    logger.LogWarning(
                        "No market data in cache for {Symbol} {MarketType}, skipping execution",
                        pipeline.Symbol,
                        pipeline.MarketType);
                    await Task.Delay(pipeline.ExecutionInterval, ct);
                    continue;
                }

                FSharpOption<MarketData>? currentData = marketDataCache.GetData(pipeline.Symbol, pipeline.MarketType);

                var context = new TradingContext
                {
                    PipelineId = PipelineId,
                    Symbol = pipeline.Symbol,
                    MarketType = pipeline.MarketType,
                    CurrentMarketData = currentData.Value,
                    CurrentPrice = recentCandlestick.Value.Close
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
                        logger.LogError(ex, "ServiceError executing step {StepName} for pipeline {PipelineId}", step.Name, PipelineId);
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
