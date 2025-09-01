using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Models;

namespace Warehouse.Backend.Core.Abstractions.Markets;

public interface IMarketAdapter
{
    string MarketName { get; }

    MarketType MarketType { get; }

    bool IsConnected { get; }

    ConnectionState ConnectionState { get; }

    event EventHandler<MarketConnectionEventArgs>? ConnectionStateChanged;

    event EventHandler<MarketErrorEventArgs>? ErrorOccurred;

    Task<ConnectionResult> ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task<bool> PingAsync(CancellationToken ct = default);

    Task<IMarketDataSubscription> SubscribeToMarketDataAsync(string symbol, CancellationToken ct = default);

    Task UnsubscribeFromMarketDataAsync(string symbol, CancellationToken ct = default);
}

public interface IMarketDataSubscription : IAsyncEnumerable<MarketData>, IDisposable
{
    string Symbol { get; }

    bool IsActive { get; }
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Failed
}

public class MarketConnectionEventArgs : EventArgs
{
    public ConnectionState OldState { get; init; }

    public ConnectionState NewState { get; init; }

    public string? Message { get; init; }
}

public class MarketErrorEventArgs : EventArgs
{
    public required string ErrorCode { get; init; }

    public required string ErrorMessage { get; init; }

    public Exception? Exception { get; init; }
}

public class ConnectionResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public static ConnectionResult CreateSuccess() => new() { Success = true };

    public static ConnectionResult CreateFailure(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
