using System.Collections.Concurrent;
using Warehouse.Core.Abstractions.Markets;

namespace Warehouse.Backend.Markets.Okx.Services;

internal sealed class OkxSubscriptionManager
{
    private readonly OkxConnectionManager connectionManager;
    private readonly ILogger<OkxSubscriptionManager> logger;
    private readonly SemaphoreSlim subscriptionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SubscriptionInfo> subscriptions = new();

    public OkxSubscriptionManager(OkxConnectionManager connectionManager, ILogger<OkxSubscriptionManager> logger)
    {
        this.connectionManager = connectionManager;
        this.logger = logger;

        connectionManager.StateChanged += OnConnectionStateChanged;
    }

    public async Task<bool> SubscribeAsync(string channel, string symbol, CancellationToken cancellationToken = default)
    {
        string key = $"{channel}:{symbol}";

        await subscriptionLock.WaitAsync(cancellationToken);
        try
        {
            if (subscriptions.ContainsKey(key))
            {
                logger.LogDebug("Already subscribed to {Key}", key);
                return true;
            }

            var request = new
            {
                op = "subscribe",
                args = new[] { new { channel, instId = symbol } }
            };

            await connectionManager.SendAsync(request, cancellationToken);

            subscriptions[key] = new SubscriptionInfo
            {
                Channel = channel,
                Symbol = symbol,
                SubscribedAt = DateTime.UtcNow
            };

            logger.LogInformation("Subscribed to {Channel} for {Symbol}", channel, symbol);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to subscribe to {Key}", key);
            return false;
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    public async Task<bool> UnsubscribeAsync(string channel, string symbol, CancellationToken cancellationToken = default)
    {
        string key = $"{channel}:{symbol}";

        await subscriptionLock.WaitAsync(cancellationToken);
        try
        {
            if (!subscriptions.TryRemove(key, out _))
            {
                logger.LogDebug("Not subscribed to {Key}", key);
                return false;
            }

            var request = new
            {
                op = "unsubscribe",
                args = new[] { new { channel, instId = symbol } }
            };

            await connectionManager.SendAsync(request, cancellationToken);

            logger.LogInformation("Unsubscribed from {Channel} for {Symbol}", channel, symbol);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unsubscribe from {Key}", key);
            return false;
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    public async Task UnsubscribeAllAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<bool>> tasks =
            subscriptions.Select(sub => UnsubscribeAsync(sub.Value.Channel, sub.Value.Symbol, cancellationToken));

        await Task.WhenAll(tasks);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (state != ConnectionState.Connected || subscriptions.IsEmpty)
        {
            return;
        }

        logger.LogInformation("Resubscribing to {Count} channels after reconnection", subscriptions.Count);

        foreach (SubscriptionInfo sub in subscriptions.Values)
        {
            try
            {
                var request = new
                {
                    op = "subscribe",
                    args = new[] { new { channel = sub.Channel, instId = sub.Symbol } }
                };

                await connectionManager.SendAsync(request);
                logger.LogDebug("Resubscribed to {Channel}:{Symbol}", sub.Channel, sub.Symbol);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resubscribe to {Channel}:{Symbol}", sub.Channel, sub.Symbol);
            }
        }
    }

    private sealed class SubscriptionInfo
    {
        public required string Channel { get; init; }

        public required string Symbol { get; init; }

        public DateTime SubscribedAt { get; init; }
    }
}
