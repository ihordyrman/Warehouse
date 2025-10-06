using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Workers.Domain;

namespace Warehouse.Core.Workers.Contracts;

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
