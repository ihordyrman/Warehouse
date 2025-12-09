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
    public override string Key => "take-profit";

    public override string Name => "Take Profit";

    public override string Description => "Closes position when profit reaches the configured threshold.";

    public override StepCategory Category => StepCategory.Signal;

    public override string Icon => "fa-chart-line";

    public override ParameterSchema GetParameterSchema()
        => new ParameterSchema()
            .AddDecimal(
                "profitPercent",
                "Profit Threshold (%)",
                "Close position when profit reaches this percentage",
                true,
                5.0m,
                0.1m,
                100m,
                "Profit Taking")
            .AddBoolean("useTrailing", "Use Trailing Stop", "Trail the stop as price rises to lock in gains", false, "Advanced")
            .AddDecimal(
                "trailingOffset",
                "Trailing Offset (%)",
                "Distance from peak price to trigger sell",
                false,
                1.0m,
                0.1m,
                50m,
                "Advanced");

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        ILogger<TakeProfitStep> logger = services.GetRequiredService<ILogger<TakeProfitStep>>();
        return new TakeProfitStep(
            parameters.GetDecimal("profitPercent", 5.0m),
            parameters.GetBoolean("useTrailing"),
            parameters.GetDecimal("trailingOffset", 1.0m),
            logger);
    }
}

/// <summary>
///     Takes profit when the configured threshold is reached.
/// </summary>
public class TakeProfitStep : IPipelineStep<TradingContext>
{
    private readonly ILogger<TakeProfitStep> logger;
    private readonly decimal profitPercent;
    private readonly decimal trailingOffset;
    private readonly bool useTrailing;

    public TakeProfitStep(decimal profitPercent, bool useTrailing, decimal trailingOffset, ILogger<TakeProfitStep> logger)
    {
        this.profitPercent = profitPercent;
        this.useTrailing = useTrailing;
        this.trailingOffset = trailingOffset;
        this.logger = logger;
    }

    public int Order => 0; // Set at runtime from database

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

        if (profitPercent >= this.profitPercent)
        {
            context.Action = TradingAction.Sell;
            logger.LogInformation("Take profit triggered at {Profit:F2}% (target: {Target:F2}%)", profitPercent, this.profitPercent);
            return Task.FromResult(PipelineStepResult.Continue($"Take profit at {profitPercent:F2}%"));
        }

        return Task.FromResult(PipelineStepResult.Continue($"Profit: {profitPercent:F2}% (target: {this.profitPercent:F2}%)"));
    }
}
