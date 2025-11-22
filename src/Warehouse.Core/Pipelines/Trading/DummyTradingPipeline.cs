using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Trading.Steps;

namespace Warehouse.Core.Pipelines.Trading;

public class DummyTradingPipeline(ILogger<DummyTradingPipeline> logger, IServiceScopeFactory serviceScopeFactory)
    : IPipeline<TradingContext>
{
    public IReadOnlyList<IPipelineStep<TradingContext>> Steps { get; } =
    [
        new CheckPositionStep(serviceScopeFactory), new ExecuteTradeStep(serviceScopeFactory),
    ];

    public async Task<PipelineResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        DateTime startTime = DateTime.UtcNow;

        try
        {
            foreach (IPipelineStep<TradingContext> step in Steps.OrderBy(x => x.Order))
            {
                if (context.IsCancelled || cancellationToken.IsCancellationRequested)
                {
                    return PipelineResult.Failure("Pipeline cancelled");
                }

                logger.LogDebug("Executing step {StepName}", step.Name);

                PipelineStepResult result = await step.ExecuteAsync(context, cancellationToken);

                if (!result.IsSuccess)
                {
                    return PipelineResult.Failure($"Step {step.Name} failed: {result.Message}");
                }

                if (!result.ShouldContinue)
                {
                    return PipelineResult.Success($"Pipeline completed at step {step.Name}: {result.Message}");
                }
            }

            return PipelineResult.Success(
                $"Pipeline completed successfully. Time taken: {(DateTime.UtcNow - startTime).TotalMilliseconds} ms");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline execution failed");
            return PipelineResult.Failure($"Pipeline failed: {ex.Message}", ex);
        }
    }
}
