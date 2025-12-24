using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Old.Functional.Orders.Contracts;
using Warehouse.Core.Old.Functional.Orders.Domain;
using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Functional.Shared;
using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Steps;

namespace Warehouse.Core.Old.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Execute Trade step.
/// </summary>
[Pipelines.Steps.StepDefinition("execute-trade")]
public class ExecuteTradeStepDefinition : BaseStepDefinition
{
    private const string ParamTradeAmount = "tradeAmount";
    private const decimal DefaultTradeAmount = 100m;

    public override string Key => "execute-trade";

    public override string Name => "Execute Trade";

    public override string Description => "Executes buy or sell orders based on the current trading action.";

    public override StepCategory Category => StepCategory.Execution;

    public override string Icon => "fa-exchange-alt";

    public override ParameterSchema GetParameterSchema()
        => new ParameterSchema().AddDecimal(
            ParamTradeAmount,
            "Trade Amount (USDT)",
            "Amount in USDT to trade per order",
            true,
            DefaultTradeAmount,
            1m,
            100000m,
            "Order Settings");

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        IServiceScopeFactory scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        return new ExecuteTradeStep(scopeFactory)
        {
            Parameters =
            {
                ["TradeAmount"] = parameters.GetDecimal(ParamTradeAmount, DefaultTradeAmount)
                    .ToString(CultureInfo.InvariantCulture)
            }
        };
    }
}

public class ExecuteTradeStep(IServiceScopeFactory serviceScopeFactory) : IPipelineStep<TradingContext>
{
    public PipelineStepType Type => PipelineStepType.Execution;

    public string Name => "ExecuteTrade";

    public Dictionary<string, string> Parameters { get; } = [];

    public async Task<PipelineStepResult> ExecuteAsync(TradingContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Action is TradingAction.None or TradingAction.Hold)
        {
            return PipelineStepResult.Stop("No trade action required");
        }

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        ILogger<ExecuteTradeStep> logger = scope.ServiceProvider.GetRequiredService<ILogger<ExecuteTradeStep>>();
        IOrderManager orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();
        IDbConnection db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

        decimal tradeAmount =
            Parameters.TryGetValue("TradeAmount", out string? amountStr) && decimal.TryParse(amountStr, out decimal amt)
                ? amt
                : throw new InvalidOperationException("Invalid trade amount parameter");

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
        IDbConnection db,
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
            Price = context.CurrentPrice // or should be null for market order? need to think about it
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

        var position = await db.QueryFirstOrDefaultAsync<Position?>(
            "SELECT * FROM positions WHERE pipeline_id = @PipelineId AND symbol = @Symbol AND status = @Status",
            new { PipelineId = context.PipelineId, Symbol = context.Symbol, Status = nameof(PositionStatus.Open) });

        if (position is not null)
        {
            position.Status = PositionStatus.Closed;
            position.ExitPrice = context.CurrentPrice;
            position.SellOrderId = result.Value.Id;

            await db.ExecuteAsync(
                "UPDATE positions SET status = @Status, exit_price = @ExitPrice  WHERE " +
                "id = @Id",
                new
                {
                    Status = position.Status.ToString(),
                    ExitPrice = position.ExitPrice,
                    SellOrderId = position.SellOrderId,
                    Id = position.Id
                });

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
        IDbConnection db,
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
            Price = context.CurrentPrice // or should be null for market order? same question as above
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

        // db.Positions.Add(position);
        // await db.SaveChangesAsync(cancellationToken);
        await db.ExecuteAsync(
            "INSERT INTO positions (pipeline_id, symbol, entry_price, quantity, status) " +
            "VALUES (@PipelineId, @Symbol, @EntryPrice, @Quantity, @Status)",
            new
            {
                PipelineId = position.PipelineId,
                Symbol = position.Symbol,
                EntryPrice = position.EntryPrice,
                Quantity = position.Quantity,
                Status = position.Status.ToString()
            });

        logger.LogInformation(
            "Opened position for {Symbol} at {Price} - Quantity: {Quantity}",
            context.Symbol,
            context.CurrentPrice,
            quantity);

        return PipelineStepResult.Continue($"Buy order executed - Price: {context.CurrentPrice:F8}");
    }
}
