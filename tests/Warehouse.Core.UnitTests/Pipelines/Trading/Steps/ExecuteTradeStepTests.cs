using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;
using Warehouse.Core.Pipelines.Trading.Steps;
using Warehouse.Core.Shared;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Trading.Steps;

public class ExecuteTradeStepTests
{
    private readonly WarehouseDbContext dbContext;
    private readonly NullLogger<ExecuteTradeStep> logger = NullLogger<ExecuteTradeStep>.Instance;
    private readonly IOrderManager orderManager;
    private readonly IServiceScopeFactory scopeFactory;

    public ExecuteTradeStepTests()
    {
        orderManager = Substitute.For<IOrderManager>();

        DbContextOptions<WarehouseDbContext> options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new WarehouseDbContext(options, null);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(orderManager);
        services.AddSingleton<ILogger<ExecuteTradeStep>>(NullLogger<ExecuteTradeStep>.Instance);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDoNothing_WhenActionIsNone()
    {
        // Arrange
        var step = new ExecuteTradeStep(scopeFactory);
        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Action = TradingAction.None
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.ShouldContinue);
        Assert.Equal("No trade action required", result.Message);

        await orderManager.DidNotReceive().CreateOrderAsync(Arg.Any<CreateOrderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBuy_WhenActionIsBuy()
    {
        // Arrange
        var step = new ExecuteTradeStep(scopeFactory)
        {
            Parameters =
            {
                ["TradeAmount"] = "100"
            }
        };

        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Action = TradingAction.Buy,
            CurrentPrice = 50000m,
            MarketType = MarketType.Okx
        };

        var order = new Order
        {
            Id = 123,
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Side = OrderSide.Buy,
            Status = OrderStatus.Filled,
            Quantity = 0.002m
        };

        orderManager.CreateOrderAsync(Arg.Any<CreateOrderRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Order>.Success(order));

        orderManager.ExecuteOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Result<Order>.Success(order));

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Buy order executed", result.Message);

        await orderManager.Received(1)
            .CreateOrderAsync(Arg.Is<CreateOrderRequest>(r => r.Side == OrderSide.Buy), Arg.Any<CancellationToken>());
        await orderManager.Received(1).ExecuteOrderAsync(123, Arg.Any<CancellationToken>());

        Position? position = await dbContext.Positions.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(position);
        Assert.Equal(PositionStatus.Open, position.Status);
        Assert.Equal(order.Id, position.BuyOrderId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSell_WhenActionIsSell()
    {
        // Arrange
        var openPosition = new Position
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Status = PositionStatus.Open,
            EntryPrice = 40000m,
            Quantity = 0.002m,
            BuyOrderId = 999
        };
        dbContext.Positions.Add(openPosition);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var step = new ExecuteTradeStep(scopeFactory);
        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Action = TradingAction.Sell,
            CurrentPrice = 60000m,
            MarketType = MarketType.Okx,
            Quantity = 0.002m,
            ActiveOrderId = 999
        };

        var order = new Order
        {
            Id = 124,
            PipelineId = 1,
            Symbol = "BTC-USDT",
            Side = OrderSide.Sell,
            Status = OrderStatus.Filled,
            Quantity = 0.002m
        };

        orderManager.CreateOrderAsync(Arg.Any<CreateOrderRequest>(), Arg.Any<CancellationToken>()).Returns(Result<Order>.Success(order));

        orderManager.ExecuteOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(Result<Order>.Success(order));

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Sell order executed", result.Message);

        await orderManager.Received(1)
            .CreateOrderAsync(Arg.Is<CreateOrderRequest>(r => r.Side == OrderSide.Sell), Arg.Any<CancellationToken>());
        await orderManager.Received(1).ExecuteOrderAsync(124, Arg.Any<CancellationToken>());

        Position? position = await dbContext.Positions.FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(position);
        Assert.Equal(PositionStatus.Closed, position.Status);
        Assert.Equal(order.Id, position.SellOrderId);
        Assert.Equal(60000m, position.ExitPrice);
    }
}
