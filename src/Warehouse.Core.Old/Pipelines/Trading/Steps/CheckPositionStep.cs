using System.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Steps;

namespace Warehouse.Core.Old.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Check Position step.
/// </summary>
[Pipelines.Steps.StepDefinition("check-position")]
public class CheckPositionStepDefinition : BaseStepDefinition
{
    public override string Key => "check-position";

    public override string Name => "Check Position";

    public override string Description => "Checks if there is an open position for this pipeline.";

    public override StepCategory Category => StepCategory.Validation;

    public override string Icon => "fa-search-dollar";

    public override ParameterSchema GetParameterSchema() => new();

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
    public PipelineStepType Type => PipelineStepType.Validation;

    public string Name => "CheckPosition";

    public Dictionary<string, string> Parameters { get; } = [];

    public async Task<PipelineStepResult> ExecuteAsync(TradingContext context,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IDbConnection db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

        var openPosition = await db.QueryFirstOrDefaultAsync<Position?>(
            "SELECT * FROM positions WHERE pipeline_id = @PipelineId AND status = @Status",
            new { PipelineId = context.PipelineId, Status = nameof(PositionStatus.Open) });

        if (openPosition is null)
        {
            context.Action = TradingAction.None;
            return PipelineStepResult.Continue("No open position");
        }

        context.BuyPrice = openPosition.EntryPrice;
        context.Quantity = openPosition.Quantity;
        context.ActiveOrderId = openPosition.BuyOrderId;
        context.Action = TradingAction.Hold;

        return PipelineStepResult.Continue($"Position found - Entry: {openPosition.EntryPrice:F8}");
    }
}
