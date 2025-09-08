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
    WarehouseDbContext dbContext,
    ILogger<OkxMarketAdapter> logger,
    IMarketDataCache dataCache) : IMarketAdapter
{
    private string Symbol { get; set; }

    public MarketType MarketType { get; }

    public bool IsConnected { get; }

    public ConnectionState ConnectionState { get; }

    public MarketData GetData()
    {
        return dataCache.GetData(Symbol);
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            MarketCredentials credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
                .FirstAsync(x => x.MarketDetails.Type == MarketType.Okx && !x.IsDemo, ct);
            httpService.Configure(credentials);
            await wsService.ConnectAsync(credentials, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OKX");
            return false;
        }

        return true;
    }

    public Task DisconnectAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public async Task SubscribeAsync(string symbol, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            throw new Exception("Connection is not established.");
        }

        if (Symbol != symbol)
        {
            throw new Exception($"Already subscribed to a {Symbol}");
        }

        MarketCredentials credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
            .FirstAsync(x => x.MarketDetails.Type == MarketType.Okx && !x.IsDemo, ct);

        await wsService.ConnectAsync(credentials, OkxChannelType.Public, ct);
        await wsService.SubscribeAsync("books", symbol, ct);
        Symbol = symbol;
    }

    public async Task UnsubscribeAsync(string symbol, CancellationToken ct = default)
        => await wsService.UnsubscribeAsync("books", symbol, ct);
}
