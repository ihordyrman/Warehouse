using Warehouse.Core.Domain;

namespace Warehouse.Core.Abstractions.Markets;

public interface IMarketAdapter
{
    MarketType MarketType { get; }

    ConnectionState ConnectionState { get; }

    Task<bool> ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task SubscribeAsync(string symbol, CancellationToken ct = default);

    Task UnsubscribeAsync(string symbol, CancellationToken ct = default);
}
