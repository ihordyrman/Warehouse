using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Abstractions.Markets;

public interface IMarketAdapter
{
    MarketType MarketType { get; }

    bool IsConnected { get; }

    ConnectionState ConnectionState { get; }

    MarketData GetData();

    Task<bool> ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task SubscribeAsync(string symbol, CancellationToken ct = default);

    Task UnsubscribeAsync(string symbol, CancellationToken ct = default);
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Failed
}
