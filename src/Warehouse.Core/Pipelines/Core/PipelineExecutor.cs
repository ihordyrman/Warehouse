using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;

namespace Warehouse.Core.Pipelines.Core;

public interface IPipelineExecutor
{
    int PipelineId { get; }

    bool IsRunning { get; }

    Task StartAsync(CancellationToken ct);

    Task StopAsync();
}

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
                var context = new TradingContext
                {
                    PipelineId = PipelineId,
                    Symbol = configuration.Symbol
                };

                foreach (PipelineStep step in configuration.Steps.OrderBy(s => s.Order))
                {
                    logger.LogDebug("Executing step {StepName} for pipeline {PipelineId}", step.Name, PipelineId);

                    // var stepInstance = CreateStep(step, scope.ServiceProvider);
                    // var result = await stepInstance.ExecuteAsync(context, ct);
                    //
                    // if (!result.ShouldContinue)
                    // {
                    //     break;
                    // }
                }

                await Task.Delay(configuration.ExecutionInterval, ct);
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
