using Warehouse.Core.Domain;

namespace Warehouse.Core.Abstractions.Workers;

public interface IMarketWorker
{
    int WorkerId { get; }

    MarketType MarketType { get; }

    bool IsRunning { get; }

    WorkerState State { get; }

    DateTime? LastProcessedAt { get; }

    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
