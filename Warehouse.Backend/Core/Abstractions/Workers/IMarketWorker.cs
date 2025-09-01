using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Abstractions.Workers;

public interface IMarketWorker
{
    int WorkerId { get; }

    MarketType MarketType { get; }

    bool IsConnected { get; }

    Task StartTradingAsync(CancellationToken ct = default);

    Task StopTradingAsync(CancellationToken ct = default);
}
