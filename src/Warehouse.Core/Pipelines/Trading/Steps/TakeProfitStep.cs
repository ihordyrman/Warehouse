using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Steps;

namespace Warehouse.Core.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Take Profit step.
/// </summary>
[StepDefinition("take-profit")]
public class TakeProfitStepDefinition : BaseStepDefinition
{
    private const string ParamProfitPercent = "profitPercent";
    private const decimal DefaultProfitPercent = 5.0m;

    private const string ParamTrailingOffset = "trailingOffset";
    private const decimal DefaultTrailingOffset = 1.0m;

    private const string ParamUseTrailing = "useTrailing";

    public override string Key => "take-profit";

    public override string Name => "Take Profit";

    public override string Description => "Closes position when profit reaches the configured threshold.";

    public override StepCategory Category => StepCategory.Signal;

    public override string Icon => "fa-chart-line";

    public override ParameterSchema GetParameterSchema()
        => new ParameterSchema()
            .AddDecimal(
                ParamProfitPercent,
                "Profit Threshold (%)",
                "Close position when profit reaches this percentage",
                true,
                DefaultProfitPercent,
                0.1m,
                100m,
                "Profit Taking")
            .AddBoolean(ParamUseTrailing, "Use Trailing Stop", "Trail the stop as price rises to lock in gains", false, "Advanced")
            .AddDecimal(
                ParamTrailingOffset,
                "Trailing Offset (%)",
                "Distance from peak price to trigger sell",
                false,
                DefaultTrailingOffset,
                0.1m,
                50m,
                "Advanced");

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        ILogger<TakeProfitStep> logger = services.GetRequiredService<ILogger<TakeProfitStep>>();
        return new TakeProfitStep(
            parameters.GetDecimal(ParamProfitPercent, DefaultProfitPercent),
            parameters.GetBoolean(ParamUseTrailing, false),
            parameters.GetDecimal(ParamTrailingOffset, DefaultTrailingOffset),
            logger);
    }
}

/// <summary>
///     Takes profit when the configured threshold is reached.
/// </summary>
public class TakeProfitStep(decimal percent, bool useTrailing, decimal trailingOffset, ILogger<TakeProfitStep> logger)
    : IPipelineStep<TradingContext>
{
    private readonly decimal trailingOffset = trailingOffset;
    private readonly bool useTrailing = useTrailing;

    public PipelineStepType Type => PipelineStepType.SignalGeneration;

    public string Name => "Take Profit";

    public Dictionary<string, string> Parameters { get; } = [];

    public Task<PipelineStepResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        if (context.BuyPrice is null)
        {
            return Task.FromResult(PipelineStepResult.Continue("No position - skipping take profit check"));
        }

        decimal profitPercent = (context.CurrentPrice - context.BuyPrice.Value) / context.BuyPrice.Value * 100;

        if (profitPercent >= percent)
        {
            context.Action = TradingAction.Sell;
            logger.LogInformation("Take profit triggered at {Profit:F2}% (target: {Target:F2}%)", profitPercent, percent);
            return Task.FromResult(PipelineStepResult.Continue($"Take profit at {profitPercent:F2}%"));
        }

        return Task.FromResult(PipelineStepResult.Continue($"Profit: {profitPercent:F2}% (target: {percent:F2}%)"));
    }
}
