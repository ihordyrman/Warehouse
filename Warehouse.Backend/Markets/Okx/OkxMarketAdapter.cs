using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx;

public class OkxMarketAdapter(
    OkxWebSocketService wsService,
    OkxHttpService httpService,
    ILogger<OkxMarketAdapter> logger,
    IMarketDataCache dataCache,
    IServiceScopeFactory serviceScopeFactory) : IMarketAdapter
{
    private string Symbol { get; set; }

    public MarketType MarketType { get; }

    public ConnectionState ConnectionState { get; private set; }

    public MarketData? GetData() => dataCache.GetData(Symbol);

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            WarehouseDbContext dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>()!;
            MarketCredentials credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
                .FirstAsync(x => x.MarketDetails.Type == MarketType.Okx && !x.IsDemo, ct);
            httpService.Configure(credentials);
            ConnectionState = ConnectionState.Connecting;
            await wsService.ConnectAsync(credentials, cancellationToken: ct);
            ConnectionState = ConnectionState.Connected;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OKX");
            ConnectionState = ConnectionState.Failed;
            return false;
        }

        return true;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await wsService.DisconnectAsync(ct);
        ConnectionState = ConnectionState.Disconnected;
    }

    public async Task SubscribeAsync(string symbol, CancellationToken ct = default)
    {
        if (ConnectionState != ConnectionState.Connected)
        {
            throw new Exception("Connection is not established.");
        }

        if (Symbol == symbol)
        {
            throw new Exception($"Already subscribed to a {Symbol}");
        }

        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        WarehouseDbContext dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>()!;
        MarketCredentials credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
            .FirstAsync(x => x.MarketDetails.Type == MarketType.Okx && !x.IsDemo, ct);

        await wsService.ConnectAsync(credentials, OkxChannelType.Public, ct);
        await wsService.SubscribeAsync("books", symbol, ct);
        Symbol = symbol;
    }

    public async Task UnsubscribeAsync(string symbol, CancellationToken ct = default)
        => await wsService.UnsubscribeAsync("books", symbol, ct);
}
