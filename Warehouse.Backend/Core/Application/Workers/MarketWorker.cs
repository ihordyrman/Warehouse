using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Abstractions.Workers;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Application.Workers;

public class MarketWorker(IMarketAdapter adapter, WorkerConfiguration configuration) : IMarketWorker
{
    public int WorkerId { get; } = configuration.WorkerId;

    public MarketType MarketType { get; } = configuration.Type;

    public bool IsConnected { get; private set; }

    public async Task StartTradingAsync(CancellationToken ct = default)
    {
        try
        {
            await adapter.ConnectAsync(ct);
            IsConnected = true;

            IMarketDataSubscription marketData = await adapter.SubscribeToMarketDataAsync(configuration.Symbol, ct);

            await foreach (MarketData data in marketData.WithCancellation(ct))
            {
                ct.ThrowIfCancellationRequested();

                // Process market data here (pipeline implementation will go here)
                // For now, just continue the loop
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            IsConnected = false;
            throw;
        }
        finally
        {
            if (IsConnected)
            {
                await StopTradingAsync(ct);
            }
        }
    }

    public async Task StopTradingAsync(CancellationToken ct = default)
    {
        if (IsConnected)
        {
            await adapter.DisconnectAsync(ct);
            IsConnected = false;
        }
    }
}
