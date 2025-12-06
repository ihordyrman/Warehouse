using Microsoft.Extensions.Logging.Abstractions;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Trading;
using Warehouse.Core.Pipelines.Trading.Steps;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Trading.Steps;

public class StopLossStepTests
{
    private readonly NullLogger<StopLossStep> logger = NullLogger<StopLossStep>.Instance;

    [Fact]
    public async Task ExecuteAsync_ShouldContinue_WhenNoPosition()
    {
        // Arrange
        var step = new StopLossStep(5.0m, logger);
        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            BuyPrice = null,
            CurrentPrice = 50000m
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("No position - skipping stop loss check", result.Message);
        Assert.Equal(TradingAction.None, context.Action);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinue_WhenLossIsBelowThreshold()
    {
        // Arrange
        const decimal thresholdPercent = 5.0m;
        var step = new StopLossStep(thresholdPercent, logger);
        const decimal buyPrice = 10000m;
        const decimal currentPrice = 9600m;

        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            BuyPrice = buyPrice,
            CurrentPrice = currentPrice
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TradingAction.None, context.Action);
        Assert.Contains("Loss: 4.00%", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSell_WhenLossExceedsThreshold()
    {
        // Arrange
        const decimal thresholdPercent = 5.0m;
        var step = new StopLossStep(thresholdPercent, logger);
        const decimal buyPrice = 10000m;
        const decimal currentPrice = 9400m;

        var context = new TradingContext
        {
            PipelineId = 1,
            Symbol = "BTC-USDT",
            BuyPrice = buyPrice,
            CurrentPrice = currentPrice
        };

        // Act
        PipelineStepResult result = await step.ExecuteAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TradingAction.Sell, context.Action);
        Assert.Contains("Stop loss at 6.00%", result.Message);
    }
}
