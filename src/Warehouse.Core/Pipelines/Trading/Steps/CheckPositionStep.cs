using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Core.Pipelines.Trading.Steps;

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

        // do we have an open position for this worker?
        Position? openPosition = await db.Positions.FirstOrDefaultAsync(
            x => x.WorkerId == context.WorkerId && x.Status == PositionStatus.Open,
            cancellationToken);

        if (openPosition is null)
        {
            context.Action = TradingAction.Buy;
            return PipelineStepResult.Continue("No open position - Buying");
        }

        context.BuyPrice = openPosition.EntryPrice;
        context.Quantity = openPosition.Quantity;
        context.ActiveOrderId = openPosition.BuyOrderId;

        // do we have 5% profit?
        decimal profitPercent = (context.CurrentPrice - openPosition.EntryPrice) / openPosition.EntryPrice * 100;
        if (profitPercent >= 5m)
        {
            context.Action = TradingAction.Sell;
            return PipelineStepResult.Continue($"Position profit: {profitPercent:F2}% - Selling");
        }

        context.Action = TradingAction.Hold;
        return PipelineStepResult.Stop($"Position profit: {profitPercent:F2}% - Holding");
    }
}
