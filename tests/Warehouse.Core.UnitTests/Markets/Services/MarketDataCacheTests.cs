using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Markets.Services;
using Xunit;

namespace Warehouse.Core.UnitTests.Markets.Services;

public class MarketDataCacheTests
{
    private readonly MarketDataCache cache = new();

    [Fact]
    public void Update_ShouldAddAsksAndBids_WhenNewDataArrives()
    {
        // Arrange
        var evt = new MarketDataEvent("BTC-USDT", MarketType.Okx, [["50000", "1.5", "0", "5"]], [["49000", "2.0", "0", "3"]]);

        // Act
        cache.Update(evt);

        // Assert
        MarketData? data = cache.GetData("BTC-USDT", MarketType.Okx);
        Assert.NotNull(data);

        Assert.Single(data.Asks);
        Assert.Contains(50000m, data.Asks.Keys);
        Assert.Equal(1.5m, data.Asks[50000m].size);
        Assert.Equal(5, data.Asks[50000m].count);

        Assert.Single(data.Bids);
        Assert.Contains(49000m, data.Bids.Keys);
        Assert.Equal(2.0m, data.Bids[49000m].size);
        Assert.Equal(3, data.Bids[49000m].count);
    }

    [Fact]
    public void Update_ShouldUpdateExistingLevels_WhenSizeChanges()
    {
        // Arrange
        var initialEvt = new MarketDataEvent("BTC-USDT", MarketType.Okx, [["50000", "1.5", "0", "5"]], []);
        cache.Update(initialEvt);

        var updateEvt = new MarketDataEvent(
            "BTC-USDT",
            MarketType.Okx,
            [["50000", "3.0", "0", "10"]], // Size changed to 3.0
            []);

        // Act
        cache.Update(updateEvt);

        // Assert
        MarketData? data = cache.GetData("BTC-USDT", MarketType.Okx);
        Assert.NotNull(data);
        Assert.Equal(3.0m, data.Asks[50000m].size);
        Assert.Equal(10, data.Asks[50000m].count);
    }

    [Fact]
    public void Update_ShouldRemoveLevels_WhenSizeIsZero()
    {
        // Arrange
        var initialEvt = new MarketDataEvent("BTC-USDT", MarketType.Okx, [["50000", "1.5", "0", "5"]], []);
        cache.Update(initialEvt);

        // Verify it's there
        Assert.Single(cache.GetData("BTC-USDT", MarketType.Okx)!.Asks);

        var removeEvt = new MarketDataEvent(
            "BTC-USDT",
            MarketType.Okx,
            [["50000", "0", "0", "0"]], // Size 0 means remove
            []);

        // Act
        cache.Update(removeEvt);

        // Assert
        MarketData? data = cache.GetData("BTC-USDT", MarketType.Okx);
        Assert.NotNull(data);
        Assert.Empty(data.Asks);
    }

    [Fact]
    public void Update_ShouldIgnoreInvalidData()
    {
        // Arrange
        var evt = new MarketDataEvent(
            "BTC-USDT",
            MarketType.Okx,
            [
                ["invalid", "1.0", "0", "1"], // Bad price
                ["50000", "bad", "0", "1"]    // Bad size
            ],
            []);

        // Act
        cache.Update(evt);

        // Assert
        MarketData? data = cache.GetData("BTC-USDT", MarketType.Okx);
        Assert.NotNull(data);
        Assert.Empty(data.Asks);
    }

    [Fact]
    public void GetData_ShouldReturnNull_WhenSymbolDoesNotExist()
    {
        // Act
        MarketData? result = cache.GetData("UNKNOWN", MarketType.Okx);

        // Assert
        Assert.Null(result);
    }
}
