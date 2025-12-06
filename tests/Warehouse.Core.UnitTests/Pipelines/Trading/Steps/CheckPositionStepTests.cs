using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;
using Warehouse.Core.Pipelines.Trading.Steps;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Trading.Steps;

public class CheckPositionStepTests
{
    private readonly WarehouseDbContext dbContext;
    private readonly IServiceScopeFactory scopeFactory;

    public CheckPositionStepTests()
    {
        DbContextOptions<WarehouseDbContext> options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        dbContext = new WarehouseDbContext(options, null);

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnContinueAndNoneAction_WhenNoOpenPosition()
    {
        // Arrange
        var step = new CheckPositionStep(scopeFactory);
        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            MarketType = MarketType.Okx
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ShouldContinue);
        Assert.Equal("No open position", result.Message);
        Assert.Equal(TradingAction.None, context.Action);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnContinueAndHoldAction_WhenPositionExists()
    {
        // Arrange
        const int pipelineId = 2;
        var existingPosition = new Position
        {
            PipelineId = pipelineId,
            Symbol = "ETH-USDT",
            Status = PositionStatus.Open,
            EntryPrice = 2000m,
            Quantity = 1.5m,
            BuyOrderId = 12345
        };
        dbContext.Positions.Add(existingPosition);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var step = new CheckPositionStep(scopeFactory);
        var context = new TradingContext
        {
            PipelineId = pipelineId,
            Symbol = "ETH-USDT",
            MarketType = MarketType.Okx
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.ShouldContinue);
        Assert.Contains("Position found", result.Message);
        Assert.Equal(TradingAction.Hold, context.Action);
        Assert.Equal(2000m, context.BuyPrice);
        Assert.Equal(1.5m, context.Quantity);
        Assert.Equal(12345, context.ActiveOrderId);
    }
}
