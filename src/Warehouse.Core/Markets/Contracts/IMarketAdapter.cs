using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Contracts;

/// <summary>
///     Standardized interface for interacting with different cryptocurrency exchanges.
///     Handles connection management and realtime data subscriptions.
/// </summary>
public interface IMarketAdapter
{
    MarketType MarketType { get; }

    ConnectionState ConnectionState { get; }

    Task<bool> ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task SubscribeAsync(string symbol, CancellationToken ct = default);

    Task UnsubscribeAsync(string symbol, CancellationToken ct = default);
}
