using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Abstractions.Workers;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Application.Workers;

public class MarketWorker(IMarketAdapter adapter, WorkerDetails details) : IMarketWorker
{
    public int WorkerId { get; } = details.WorkerId;

    public MarketType MarketType { get; } = details.Type;

    public bool IsConnected { get; private set; }

    public async Task StartTradingAsync(CancellationToken ct = default)
    {
        await adapter.ConnectAsync(ct);
        IsConnected = true;

        IMarketDataSubscription marketData = await adapter.SubscribeToMarketDataAsync(details.Symbol, ct);
        await foreach (MarketData cw in marketData)
        {
            // here will be an implementation with pipelines
        }
    }

    public async Task StopTradingAsync(CancellationToken ct = default)
    {
        await adapter.DisconnectAsync(ct);
        IsConnected = false;
    }
}
