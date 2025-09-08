using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Abstractions.Workers;

public interface IMarketWorker
{
    int WorkerId { get; }

    MarketType MarketType { get; }

    bool IsConnected { get; }

    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
