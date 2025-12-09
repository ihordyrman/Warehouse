using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;
using Warehouse.Core.Shared.Services;
using Xunit;

namespace Warehouse.Core.UnitTests.Shared.Services;

public class CandlestickServiceTests
{
    private readonly WarehouseDbContext dbContext;
    private readonly CandlestickService service;

    public CandlestickServiceTests()
    {
        DbContextOptions<WarehouseDbContext> options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        dbContext = new WarehouseDbContext(options, null);
        service = new CandlestickService(dbContext, NullLogger<CandlestickService>.Instance);
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldReturnZero_WhenEmptyCollection()
    {
        // Arrange, Act
        int result = await service.SaveCandlesticksAsync([], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
        Assert.Empty(dbContext.Candlesticks);
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldAddNewCandlesticks()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        var candlesticks = new List<Candlestick>
        {
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp.AddMinutes(1), 50500m, 52000m, 50000m, 51500m)
        };

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(2, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldUpdateIncompleteCandlestick_WhenNewDataArrives()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        Candlestick existingCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            50500m,
            49500m,
            50200m,
            isCompleted: false);
        dbContext.Candlesticks.Add(existingCandle);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Candlestick updatedCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            51000m,
            49000m,
            50800m,
            isCompleted: true);

        // Act
        int result = await service.SaveCandlesticksAsync([updatedCandle], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result);
        Assert.Equal(1, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));

        Candlestick saved = await dbContext.Candlesticks.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(51000m, saved.High);
        Assert.Equal(49000m, saved.Low);
        Assert.Equal(50800m, saved.Close);
        Assert.True(saved.IsCompleted);
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldSkipUpdate_WhenBothCandlesAreCompleted()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        Candlestick existingCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            51000m,
            49000m,
            50500m,
            isCompleted: true);
        dbContext.Candlesticks.Add(existingCandle);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Candlestick newCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            99999m,
            99999m,
            99999m,
            99999m,
            isCompleted: true);

        // Act
        int result = await service.SaveCandlesticksAsync([newCandle], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);

        Candlestick saved = await dbContext.Candlesticks.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(50000m, saved.Open);
        Assert.Equal(51000m, saved.High);
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldHandleMultipleSymbols()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        var candlesticks = new List<Candlestick>
        {
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("ETH-USDT", MarketType.Okx, "1m", timestamp, 3000m, 3100m, 2900m, 3050m),
            CreateCandlestick("SOL-USDT", MarketType.Okx, "1m", timestamp, 100m, 110m, 90m, 105m)
        };

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result);
        Assert.Equal(3, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldHandleMultipleTimeframes()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        var candlesticks = new List<Candlestick>
        {
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "5m", timestamp, 50000m, 52000m, 48000m, 51000m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1H", timestamp, 50000m, 55000m, 45000m, 52000m)
        };

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result);
        Assert.Equal(3, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldHandleMultipleMarketTypes()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        var candlesticks = new List<Candlestick>
        {
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Binance, "1m", timestamp, 50010m, 51010m, 49010m, 50510m)
        };

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(2, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldHandleMixedAddAndUpdate()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        Candlestick existingCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            50500m,
            49500m,
            50200m,
            isCompleted: false);
        dbContext.Candlesticks.Add(existingCandle);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var candlesticks = new List<Candlestick>
        {
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50800m, isCompleted: true),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp.AddMinutes(1), 50800m, 52000m, 50500m, 51500m)
        };

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(2, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldUpdateVolumeFields()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        Candlestick existingCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            50500m,
            49500m,
            50200m,
            isCompleted: false,
            volume: 100m,
            volumeQuote: 5000000m);
        dbContext.Candlesticks.Add(existingCandle);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Candlestick updatedCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            51000m,
            49000m,
            50800m,
            isCompleted: true,
            volume: 250m,
            volumeQuote: 12500000m);

        // Act
        await service.SaveCandlesticksAsync([updatedCandle], TestContext.Current.CancellationToken);

        // Assert
        Candlestick saved = await dbContext.Candlesticks.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(250m, saved.Volume);
        Assert.Equal(12500000m, saved.VolumeQuote);
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldHandleLargeBatch()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        var candlesticks = Enumerable.Range(0, 1000)
            .Select(i => CreateCandlestick(
                        "BTC-USDT",
                        MarketType.Okx,
                        "1m",
                        timestamp.AddMinutes(i),
                        50000m + i,
                        51000m + i,
                        49000m + i,
                        50500m + i))
            .ToList();

        // Act
        int result = await service.SaveCandlesticksAsync(candlesticks, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1000, result);
        Assert.Equal(1000, await dbContext.Candlesticks.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCandlesticksAsync_ShouldUpdateIncompleteCandle_WhenNewIncompleteArrives()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        Candlestick existingCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            50500m,
            49500m,
            50200m,
            isCompleted: false);
        dbContext.Candlesticks.Add(existingCandle);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Candlestick updatedCandle = CreateCandlestick(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            timestamp,
            50000m,
            50800m,
            49200m,
            50600m,
            isCompleted: false);

        // Act
        int result = await service.SaveCandlesticksAsync([updatedCandle], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result);
        Candlestick saved = await dbContext.Candlesticks.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(50800m, saved.High);
        Assert.Equal(49200m, saved.Low);
        Assert.False(saved.IsCompleted);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldReturnEmpty_WhenNoCandlesticksExist()
    {
        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m"))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldReturnMatchingCandlesticks()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("ETH-USDT", MarketType.Okx, "1m", timestamp, 3000m, 3100m, 2900m, 3050m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m"))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal("BTC-USDT", x.Symbol));
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldFilterByMarketType()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Binance, "1m", timestamp, 50010m, 51010m, 49010m, 50510m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m"))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Single(result);
        Assert.Equal(MarketType.Okx, result[0].MarketType);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldFilterByTimeframe()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "5m", timestamp, 50000m, 52000m, 48000m, 51000m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "5m"))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Single(result);
        Assert.Equal("5m", result[0].Timeframe);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldFilterByFromDate()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 51500m, 53000m, 51000m, 52500m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m", from: baseTime.AddMinutes(1)))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.True(x.Timestamp >= baseTime.AddMinutes(1)));
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldFilterByToDate()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 51500m, 53000m, 51000m, 52500m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m", to: baseTime.AddMinutes(1)))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.True(x.Timestamp <= baseTime.AddMinutes(1)));
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 51500m, 53000m, 51000m, 52500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(3), 52500m, 54000m, 52000m, 53500m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync(
                           "BTC-USDT",
                           MarketType.Okx,
                           "1m",
                           from: baseTime.AddMinutes(1),
                           to: baseTime.AddMinutes(2)))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldApplyLimit()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 10; i++)
        {
            dbContext.Candlesticks.Add(
                CreateCandlestick(
                    "BTC-USDT",
                    MarketType.Okx,
                    "1m",
                    baseTime.AddMinutes(i),
                    50000m + i * 100,
                    51000m + i * 100,
                    49000m + i * 100,
                    50500m + i * 100));
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m", limit: 5))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldReturnInDescendingOrder()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 51500m, 53000m, 51000m, 52500m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync("BTC-USDT", MarketType.Okx, "1m"))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].Timestamp > result[1].Timestamp);
        Assert.True(result[1].Timestamp > result[2].Timestamp);
    }

    [Fact]
    public async Task GetCandlesticksAsync_ShouldCombineAllFilters()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 20; i++)
        {
            dbContext.Candlesticks.Add(
                CreateCandlestick(
                    "BTC-USDT",
                    MarketType.Okx,
                    "1m",
                    baseTime.AddMinutes(i),
                    50000m + i * 100,
                    51000m + i * 100,
                    49000m + i * 100,
                    50500m + i * 100));
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = new List<Candlestick>();
        await foreach (Candlestick candle in service.GetCandlesticksAsync(
                           "BTC-USDT",
                           MarketType.Okx,
                           "1m",
                           from: baseTime.AddMinutes(5),
                           to: baseTime.AddMinutes(15),
                           limit: 5))
        {
            result.Add(candle);
        }

        // Assert
        Assert.Equal(5, result.Count);
        Assert.All(
            result,
            x =>
            {
                Assert.True(x.Timestamp >= baseTime.AddMinutes(5));
                Assert.True(x.Timestamp <= baseTime.AddMinutes(15));
            });
    }

    [Fact]
    public async Task GetLatestCandlestickAsync_ShouldReturnNull_WhenNoCandlesticksExist()
    {
        // Act
        Candlestick? result = await service.GetLatestCandlestickAsync(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestCandlestickAsync_ShouldReturnMostRecentCandlestick()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(1), 50500m, 52000m, 50000m, 51500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 51500m, 53000m, 51000m, 52500m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Candlestick? result = await service.GetLatestCandlestickAsync(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(baseTime.AddMinutes(2), result.Timestamp);
        Assert.Equal(52500m, result.Close);
    }

    [Fact]
    public async Task GetLatestCandlestickAsync_ShouldFilterBySymbol()
    {
        // Arrange
        DateTime timestamp = DateTime.UtcNow;
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", timestamp, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("ETH-USDT", MarketType.Okx, "1m", timestamp.AddMinutes(1), 3000m, 3100m, 2900m, 3050m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Candlestick? result = await service.GetLatestCandlestickAsync(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BTC-USDT", result.Symbol);
        Assert.Equal(50500m, result.Close);
    }

    [Fact]
    public async Task GetLatestCandlestickAsync_ShouldFilterByMarketType()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime, 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Binance, "1m", baseTime.AddMinutes(1), 50010m, 51010m, 49010m, 50510m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Candlestick? result = await service.GetLatestCandlestickAsync(
            "BTC-USDT",
            MarketType.Okx,
            "1m",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MarketType.Okx, result.MarketType);
        Assert.Equal(50500m, result.Close);
    }

    [Fact]
    public async Task GetLatestCandlestickAsync_ShouldFilterByTimeframe()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        dbContext.Candlesticks.AddRange(
            CreateCandlestick("BTC-USDT", MarketType.Okx, "1m", baseTime.AddMinutes(2), 50000m, 51000m, 49000m, 50500m),
            CreateCandlestick("BTC-USDT", MarketType.Okx, "5m", baseTime, 50000m, 52000m, 48000m, 51000m));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Candlestick? result = await service.GetLatestCandlestickAsync(
            "BTC-USDT",
            MarketType.Okx,
            "5m",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("5m", result.Timeframe);
        Assert.Equal(51000m, result.Close);
    }

    private static Candlestick CreateCandlestick(
        string symbol,
        MarketType marketType,
        string timeframe,
        DateTime timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        bool isCompleted = true,
        decimal volume = 1000m,
        decimal volumeQuote = 50000000m)
        => new()
        {
            Symbol = symbol,
            MarketType = marketType,
            Timeframe = timeframe,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
            VolumeQuote = volumeQuote,
            IsCompleted = isCompleted
        };
}
