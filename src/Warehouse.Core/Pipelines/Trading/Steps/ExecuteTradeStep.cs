using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Pipelines.Trading.Steps;

public class ExecuteTradeStep(IServiceScopeFactory serviceScopeFactory) : IPipelineStep<TradingContext>
{
    public int Order => 2;

    public PipelineStepType Type => PipelineStepType.Execution;

    public string Name => "ExecuteTrade";

    public Dictionary<string, string> Parameters { get; set; } = [];

    public async Task<PipelineStepResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        if (context.Action is TradingAction.None or TradingAction.Hold)
        {
            return PipelineStepResult.Stop("No trade action required");
        }

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ILogger<ExecuteTradeStep> logger = scope.ServiceProvider.GetRequiredService<ILogger<ExecuteTradeStep>>();
        IOrderManager orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();
        WarehouseDbContext db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

        decimal tradeAmount = Parameters.TryGetValue("TradeAmount", out string? amountStr) && decimal.TryParse(amountStr, out decimal amt) ?
            amt :
            100m;

        if (context.Action == TradingAction.Buy)
        {
            return await Buy(context, tradeAmount, orderManager, db, logger, cancellationToken);
        }

        if (context is { Action: TradingAction.Sell, Quantity: not null, ActiveOrderId: not null })
        {
            return await Sell(context, orderManager, db, logger, cancellationToken);
        }

        return PipelineStepResult.Stop("No action needed");
    }

    private static async Task<PipelineStepResult> Sell(
        TradingContext context,
        IOrderManager orderManager,
        WarehouseDbContext db,
        ILogger<ExecuteTradeStep> logger,
        CancellationToken cancellationToken)
    {
        var request = new CreateOrderRequest
        {
            PipelineId = context.PipelineId,
            Side = OrderSide.Sell,
            Quantity = context.Quantity!.Value,
            MarketType = context.MarketType,
            Symbol = context.Symbol,
            Price = context.CurrentPrice // or should be null for market order?
        };

        Result<Order> result = await orderManager.CreateOrderAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return PipelineStepResult.Error($"Failed to create sell order: {result.Error.Message}");
        }

        Result<Order> executeResult = await orderManager.ExecuteOrderAsync(result.Value.Id, cancellationToken);
        if (!executeResult.IsSuccess)
        {
            return PipelineStepResult.Error($"Failed to execute sell order: {executeResult.Error.Message}");
        }

        Position? position = await db.Positions.FirstOrDefaultAsync(
            x => x.PipelineId == context.PipelineId && x.Symbol == context.Symbol && x.Status == PositionStatus.Open,
            cancellationToken);

        if (position is not null)
        {
            position.Status = PositionStatus.Closed;
            position.ExitPrice = context.CurrentPrice;
            position.SellOrderId = result.Value.Id;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Closed position for {Symbol} at {Price} - Quantity: {Quantity}",
                context.Symbol,
                context.CurrentPrice,
                context.Quantity.Value);
        }

        return PipelineStepResult.Continue($"Sell order executed - Price: {context.CurrentPrice:F8}");
    }

    private static async Task<PipelineStepResult> Buy(
        TradingContext context,
        decimal tradeAmount,
        IOrderManager orderManager,
        WarehouseDbContext db,
        ILogger<ExecuteTradeStep> logger,
        CancellationToken cancellationToken)
    {
        decimal quantity = tradeAmount / context.CurrentPrice;
        var request = new CreateOrderRequest
        {
            PipelineId = context.PipelineId,
            Side = OrderSide.Buy,
            Quantity = quantity,
            MarketType = context.MarketType,
            Symbol = context.Symbol,
            Price = context.CurrentPrice // or should be null for market order?
        };

        Result<Order> result = await orderManager.CreateOrderAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return PipelineStepResult.Error($"Failed to create buy order: {result.Error.Message}");
        }

        Result<Order> executeResult = await orderManager.ExecuteOrderAsync(result.Value.Id, cancellationToken);
        if (!executeResult.IsSuccess)
        {
            return PipelineStepResult.Error($"Failed to execute buy order: {executeResult.Error.Message}");
        }

        var position = new Position
        {
            PipelineId = context.PipelineId,
            Symbol = context.Symbol,
            EntryPrice = context.CurrentPrice,
            Quantity = quantity,
            BuyOrderId = result.Value.Id,
            Status = PositionStatus.Open
        };

        db.Positions.Add(position);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Opened position for {Symbol} at {Price} - Quantity: {Quantity}",
            context.Symbol,
            context.CurrentPrice,
            quantity);

        return PipelineStepResult.Continue($"Buy order executed - Price: {context.CurrentPrice:F8}");
    }
}
