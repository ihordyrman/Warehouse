using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Steps;

namespace Warehouse.Core.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Check Position step.
/// </summary>
[StepDefinition("check-position")]
public class CheckPositionStepDefinition : BaseStepDefinition
{
    public override string Key => "check-position";

    public override string Name => "Check Position";

    public override string Description => "Checks if there is an open position for this pipeline.";

    public override StepCategory Category => StepCategory.Validation;

    public override string Icon => "fa-search-dollar";

    public override ParameterSchema GetParameterSchema()
        =>

            // This step has no configurable parameters
            new();

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        IServiceScopeFactory scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        return new CheckPositionStep(scopeFactory);
    }
}

/// <summary>
///     Checks if there is an open position for this pipeline and sets context accordingly.
/// </summary>
public class CheckPositionStep(IServiceScopeFactory serviceScopeFactory) : IPipelineStep<TradingContext>
{
    public int Order => 1;

    public PipelineStepType Type => PipelineStepType.Validation;

    public string Name => "CheckPosition";

    public Dictionary<string, string> Parameters { get; } = [];

    public async Task<PipelineStepResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        WarehouseDbContext db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        // Check for an open position for this pipeline
        Position? openPosition = await db.Positions.FirstOrDefaultAsync(
            x => x.PipelineId == context.PipelineId && x.Status == PositionStatus.Open,
            cancellationToken);

        if (openPosition is null)
        {
            // No position - signal steps should decide whether to buy
            context.Action = TradingAction.None;
            return PipelineStepResult.Continue("No open position");
        }

        // Set context with position info for downstream steps
        context.BuyPrice = openPosition.EntryPrice;
        context.Quantity = openPosition.Quantity;
        context.ActiveOrderId = openPosition.BuyOrderId;
        context.Action = TradingAction.Hold;

        return PipelineStepResult.Continue($"Position found - Entry: {openPosition.EntryPrice:F8}");
    }
}
