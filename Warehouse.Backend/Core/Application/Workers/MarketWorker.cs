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

    public async Task StartAsync(CancellationToken ct = default)
    {
        try
        {
            await adapter.ConnectAsync(ct);
            IsConnected = true;

            await adapter.SubscribeAsync(configuration.Symbol, ct);

            // todo: start analyzing data periodically
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
                await StopAsync(ct);
            }
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (IsConnected)
        {
            await adapter.DisconnectAsync(ct);
            IsConnected = false;
        }
    }
}
