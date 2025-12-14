using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Steps;

namespace Warehouse.Core.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Stop Loss step.
/// </summary>
[StepDefinition("stop-loss")]
public class StopLossStepDefinition : BaseStepDefinition
{
    private const string ParamLossPercent = "lossPercent";
    private const decimal DefaultLossPercent = 3.0m;

    public override string Key => "stop-loss";

    public override string Name => "Stop Loss";

    public override string Description => "Closes position when loss reaches the configured threshold to limit downside.";

    public override StepCategory Category => StepCategory.Signal;

    public override string Icon => "fa-ban";

    public override ParameterSchema GetParameterSchema()
        => new ParameterSchema().AddDecimal(
            ParamLossPercent,
            "Loss Threshold (%)",
            "Close position when loss reaches this percentage",
            true,
            DefaultLossPercent,
            0.1m,
            100m,
            "Stop Loss");

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        ILogger<StopLossStep> logger = services.GetRequiredService<ILogger<StopLossStep>>();
        return new StopLossStep(parameters.GetDecimal(ParamLossPercent, DefaultLossPercent), logger);
    }
}

/// <summary>
///     Triggers stop loss when the configured threshold is reached.
/// </summary>
public class StopLossStep(decimal percent, ILogger<StopLossStep> logger) : IPipelineStep<TradingContext>
{
    public PipelineStepType Type => PipelineStepType.SignalGeneration;

    public string Name => "Stop Loss";

    public Dictionary<string, string> Parameters { get; } = [];

    public Task<PipelineStepResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        if (context.BuyPrice is null)
        {
            return Task.FromResult(PipelineStepResult.Continue("No position - skipping stop loss check"));
        }

        decimal lossPercent = (context.BuyPrice.Value - context.CurrentPrice) / context.BuyPrice.Value * 100;

        if (lossPercent >= percent)
        {
            context.Action = TradingAction.Sell;
            logger.LogWarning("Stop loss triggered at {Loss:F2}% (threshold: {Threshold:F2}%)", lossPercent, percent);
            return Task.FromResult(PipelineStepResult.Continue($"Stop loss at {lossPercent:F2}%"));
        }

        return Task.FromResult(PipelineStepResult.Continue($"Loss: {lossPercent:F2}% (threshold: {percent:F2}%)"));
    }
}
