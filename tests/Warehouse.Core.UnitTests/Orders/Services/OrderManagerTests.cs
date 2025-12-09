using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Orders.Services;
using Warehouse.Core.Shared;
using Xunit;

namespace Warehouse.Core.UnitTests.Orders.Services;

public class OrderManagerTests
{
    private readonly WarehouseDbContext dbContext;
    private readonly IMarketOrderProvider marketProvider;
    private readonly OrderManager orderManager;

    public OrderManagerTests()
    {
        DbContextOptions<WarehouseDbContext> options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        dbContext = new WarehouseDbContext(options, null);

        marketProvider = Substitute.For<IMarketOrderProvider>();

        var services = new ServiceCollection();
        services.AddSingleton(marketProvider);

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        orderManager = new OrderManager(dbContext, NullLogger<OrderManager>.Instance, serviceProvider);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldPersistOrder()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            PipelineId = 1,
            MarketType = MarketType.Okx,
            Symbol = "BTC-USDT",
            Side = OrderSide.Buy,
            Quantity = 0.5m,
            Price = 50000m
        };

        // Act
        Result<Order> result = await orderManager.CreateOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        Order? order = await dbContext.Orders.FindAsync([result.Value.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0.5m, order.Quantity);
    }

    [Fact]
    public async Task ExecuteOrderAsync_ShouldPlaceOrder_WhenProviderSucceeds()
    {
        // Arrange
        var order = new Order
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            MarketType = MarketType.Okx,
            Status = OrderStatus.Pending,
            Quantity = 1m
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        marketProvider.ExecuteOrderAsync(Arg.Is<Order>(o => o.Id == order.Id), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("okx-123"));

        // Act
        Result<Order> result = await orderManager.ExecuteOrderAsync(order.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Placed, result.Value.Status);
        Assert.Equal("okx-123", result.Value.ExchangeOrderId);

        Order? dbOrder = await dbContext.Orders.FindAsync([order.Id], TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, dbOrder!.Status);
    }

    [Fact]
    public async Task ExecuteOrderAsync_ShouldFailOrder_WhenProviderFails()
    {
        // Arrange
        var order = new Order
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            MarketType = MarketType.Okx,
            Status = OrderStatus.Pending
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        marketProvider.ExecuteOrderAsync(Arg.Is<Order>(o => o.Id == order.Id), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(new Error("API Error")));

        // Act
        Result<Order> result = await orderManager.ExecuteOrderAsync(order.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(OrderStatus.Failed, order.Status);

        Order? dbOrder = await dbContext.Orders.FindAsync([order.Id], TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Failed, dbOrder!.Status);
    }

    [Fact]
    public async Task ExecuteOrderAsync_ShouldFail_WhenOrderIsNotPending()
    {
        // Arrange
        var order = new Order
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            MarketType = MarketType.Okx,
            Status = OrderStatus.Filled
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Result<Order> result = await orderManager.ExecuteOrderAsync(order.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot place order in status", result.Error.Message);
    }
}
