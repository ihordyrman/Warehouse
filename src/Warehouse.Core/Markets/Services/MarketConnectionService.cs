using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Concrete.Okx;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Core.Markets.Services;

/// <summary>
///     Background service that maintains connections to active markets.
///     Manages lifecycle of connections, including connecting, disconnecting, and updating subscriptions based on active pipelines.
/// </summary>
public class MarketConnectionService(ILogger<MarketConnectionService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private const int PollingIntervalSeconds = 5;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly Dictionary<MarketType, MarketConnection> marketConnections = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                WarehouseDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();

                List<Pipeline> activePipelines = await dbContext.PipelineConfigurations.Where(x => x.Enabled)
                    .Select(x => new Pipeline
                    {
                        Symbol = x.Symbol,
                        MarketType = x.MarketType
                    })
                    .ToListAsync(stoppingToken);

                var pipelinesByMarket = activePipelines.GroupBy(x => x.MarketType).ToDictionary(x => x.Key, g => g.ToList());

                foreach ((MarketType marketType, List<Pipeline> pipelines) in pipelinesByMarket)
                {
                    await EnsureMarketConnectionAsync(marketType, pipelines, stoppingToken);
                }

                await DisconnectUnusedMarketsAsync(activePipelines);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in MarketConnectionService execution loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task UpdateSubscriptionsAsync(MarketConnection connection, List<Pipeline> pipelines, CancellationToken cancellationToken)
    {
        var requiredSymbols = pipelines.Select(x => x.Symbol).Distinct().ToHashSet();

        IEnumerable<string> newSymbols = requiredSymbols.Except(connection.Symbols);
        foreach (string symbol in newSymbols)
        {
            logger.LogInformation("Adding subscription for {Symbol} on {MarketType}", symbol, connection.MarketType);
            await connection.Adapter.SubscribeAsync(symbol, cancellationToken);
            connection.Symbols.Add(symbol);
        }

        IEnumerable<string> unusedSymbols = connection.Symbols.Except(requiredSymbols);
        foreach (string symbol in unusedSymbols)
        {
            logger.LogInformation("Removing subscription for {Symbol} on {MarketType}", symbol, connection.MarketType);
            await connection.Adapter.UnsubscribeAsync(symbol, cancellationToken);
            connection.Symbols.Remove(symbol);
        }
    }

    private async Task DisconnectUnusedMarketsAsync(List<Pipeline> activePipelines)
    {
        var activeMarkets = activePipelines.Select(x => x.MarketType).Distinct().ToHashSet();
        var marketsToDisconnect = marketConnections.Keys.Where(x => !activeMarkets.Contains(x)).ToList();

        foreach (MarketType marketType in marketsToDisconnect)
        {
            await DisconnectMarketAsync(marketType);
        }
    }

    private async Task DisconnectMarketAsync(MarketType marketType)
    {
        await connectionLock.WaitAsync();
        try
        {
            if (!marketConnections.TryGetValue(marketType, out MarketConnection? connection))
            {
                return;
            }

            logger.LogInformation("Disconnecting from {MarketType}", marketType);

            try
            {
                await connection.Adapter.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disconnecting from {MarketType}", marketType);
            }

            marketConnections.Remove(marketType);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private async Task EnsureMarketConnectionAsync(MarketType marketType, List<Pipeline> pipelines, CancellationToken cancellationToken)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (marketConnections.TryGetValue(marketType, out MarketConnection? connection) && connection.IsConnected)
            {
                await UpdateSubscriptionsAsync(connection, pipelines, cancellationToken);
                return;
            }

            logger.LogInformation("Establishing connection to {MarketType}", marketType);

            IMarketAdapter adapter = CreateMarketAdapter(marketType);
            await adapter.ConnectAsync(cancellationToken);

            var symbols = pipelines.Select(x => x.Symbol).Distinct().ToList();
            foreach (string symbol in symbols)
            {
                await adapter.SubscribeAsync(symbol, cancellationToken);
            }

            marketConnections[marketType] = new MarketConnection
            {
                MarketType = marketType,
                Adapter = adapter,
                Symbols = symbols.ToHashSet(),
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow
            };

            logger.LogInformation("Successfully connected to {MarketType} with {SymbolCount} symbols", marketType, symbols.Count);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private IMarketAdapter CreateMarketAdapter(MarketType marketType)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();

        return marketType switch
        {
            MarketType.Okx => scope.ServiceProvider.GetRequiredService<OkxMarketAdapter>(),
            _ => throw new NotSupportedException($"Market type {marketType} is not supported")
        };
    }

    private class MarketConnection
    {
        public MarketType MarketType { get; init; }

        public IMarketAdapter Adapter { get; init; } = null!;

        public HashSet<string> Symbols { get; init; } = [];

        public bool IsConnected { get; init; }

        public DateTime ConnectedAt { get; set; }
    }
}
