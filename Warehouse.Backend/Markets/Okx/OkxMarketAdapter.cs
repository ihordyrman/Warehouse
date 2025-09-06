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
    ILogger<OkxMarketAdapter> logger) : IMarketAdapter
{
    public static MarketType Type => MarketType.Okx;

    public string MarketName { get; }

    public MarketType MarketType { get; }

    public bool IsConnected => wsService.IsConnected;

    public ConnectionState ConnectionState { get; }

    public event EventHandler<MarketConnectionEventArgs>? ConnectionStateChanged;

    public event EventHandler<MarketErrorEventArgs>? ErrorOccurred;

    public async Task<ConnectionResult> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            MarketCredentials? credentials = await dbContext.MarketCredentials.Include(x => x.MarketDetails)
                .FirstOrDefaultAsync(x => x.MarketDetails.Type == MarketType.Okx && !x.IsDemo, ct);
            if (credentials is null)
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Credentials for Okx market are not found"
                };
            }

            httpService.Configure(credentials);
            await wsService.ConnectAsync(credentials, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OKX");
        }

        return new ConnectionResult { Success = true };
    }

    public Task DisconnectAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<bool> PingAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<IMarketDataSubscription> SubscribeToMarketDataAsync(string symbol, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task UnsubscribeFromMarketDataAsync(string symbol, CancellationToken ct = default) => throw new NotImplementedException();

    public IAsyncEnumerable<MarketData> StreamMarketDataAsync(string symbol, CancellationToken ct = default)
        => throw new NotImplementedException();
}
