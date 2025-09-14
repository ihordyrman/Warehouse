using System.Collections.Concurrent;
using System.Threading.Channels;
using Warehouse.Backend.Core.Abstractions.EventBus;

namespace Warehouse.Backend.Core.Application.EventBus;

public class InMemoryEventBus : IEventBus, IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Channel<EventEnvelope> eventChannel = Channel.CreateBounded<EventEnvelope>(
        new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    private readonly ILogger<InMemoryEventBus> logger;
    private readonly Task processingTask;
    private readonly SemaphoreSlim semaphore = new(1);
    private readonly IServiceProvider serviceProvider;
    private readonly ConcurrentDictionary<Type, List<ISubscription>> subscriptions = [];

    private bool disposed;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        processingTask = Task.Run(async () => await ProcessEventsAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        try
        {
            semaphore.Wait();
            if (disposed)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            eventChannel.Writer.TryComplete();

            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error waiting for event processing to complete");
            }

            cancellationTokenSource.Dispose();
            disposed = true;
        }
        finally
        {
            semaphore.Release();
            semaphore.Dispose();
        }
    }

    public async Task PublishAsync<T>(T eventData, CancellationToken ct = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ThrowIfDisposed();

        Type eventType = typeof(T);
        logger.LogDebug("Publishing event of type {EventType}", eventType.Name);

        var envelope = new EventEnvelope(eventData, eventType, DateTime.UtcNow);
        await eventChannel.Writer.WriteAsync(envelope, ct);
    }

    public IDisposable Subscribe<T>(Action<T> handler)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        var subscription = new ActionSubscription<T>(handler, RemoveSubscription);
        AddSubscription(typeof(T), subscription);

        logger.LogDebug("Added subscription for {EventType}", typeof(T).Name);
        return subscription;
    }

    public IDisposable Subscribe<T>(Func<T, Task> handler)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        var subscription = new AsyncSubscription<T>(handler, RemoveSubscription);
        AddSubscription(typeof(T), subscription);

        logger.LogDebug("Added subscription for {EventType}", typeof(T).Name);
        return subscription;
    }

    public IDisposable Subscribe<T, THandler>()
        where T : class
        where THandler : IEventHandler<T>
    {
        ThrowIfDisposed();

        var subscription = new TypedSubscription<T, THandler>(serviceProvider, RemoveSubscription);
        AddSubscription(typeof(T), subscription);

        logger.LogDebug("Added typed subscription for {EventType} with handler {HandlerType}", typeof(T).Name, typeof(THandler).Name);
        return subscription;
    }

    private void AddSubscription(Type eventType, ISubscription subscription)
    {
        semaphore.Wait();
        try
        {
            subscriptions.AddOrUpdate(
                eventType,
                [subscription],
                (key, list) =>
                {
                    list.Add(subscription);
                    return list;
                });
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void RemoveSubscription(Type eventType, ISubscription subscription)
    {
        semaphore.Wait();
        try
        {
            if (!subscriptions.TryGetValue(eventType, out List<ISubscription>? subs))
            {
                return;
            }

            subs.Remove(subscription);
            if (subscriptions.IsEmpty)
            {
                subscriptions.TryRemove(eventType, out _);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessEventEnvelope(EventEnvelope envelope, CancellationToken ct)
    {
        if (!subscriptions.TryGetValue(envelope.EventType, out List<ISubscription>? subs))
        {
            logger.LogWarning("No subscriptions found for event type {EventType}", envelope.EventType.Name);
            return;
        }

        var subscriptionsCopy = subs.ToList();

        await Parallel.ForEachAsync(
            subscriptionsCopy,
            ct,
            async (subscription, token) =>
            {
                await ExecuteSubscription(subscription, envelope, token);
            });
    }

    private async Task ExecuteSubscription(ISubscription subscription, EventEnvelope envelope, CancellationToken ct)
    {
        try
        {
            await subscription.HandleAsync(envelope.EventData, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing subscription for event type {EventType}", envelope.EventType.Name);
            throw;
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (EventEnvelope envelope in eventChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    logger.LogTrace(
                        "Processing event of type {EventType} published at {Timestamp}",
                        envelope.EventType.Name,
                        envelope.Timestamp);

                    await ProcessEventEnvelope(envelope, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error processing event of type {EventType}. Event will be skipped.", envelope.EventType.Name);

                    try
                    {
                        await HandleDeadLetterAsync(envelope, ex, cancellationToken);
                    }
                    catch (Exception dlqEx)
                    {
                        logger.LogError(dlqEx, "Failed to send event to dead letter queue");
                    }
                }
            }

            logger.LogInformation("EventBus reader loop ended normally (channel completed)");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("EventBus reader task cancelled, shutting down gracefully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "EventBus reader task crashed unexpectedly!");
            throw;
        }
        finally
        {
            logger.LogDebug("EventBus reader task exiting");
        }
    }

    private async Task HandleDeadLetterAsync(EventEnvelope envelope, Exception exception, CancellationToken ct)
    {
        logger.LogWarning("Event {EventType} moved to dead letter queue. Error: {Error}", envelope.EventType.Name, exception.Message);

        await PublishAsync(
            new DeadLetterEvent
            {
                OriginalEventType = envelope.EventType,
                OriginalEventData = envelope.EventData,
                Message = exception.Message,
                Timestamp = DateTime.UtcNow
            },
            ct);
    }

    private void ThrowIfDisposed()
    {
        if (!disposed)
        {
            return;
        }

        throw new ObjectDisposedException(nameof(InMemoryEventBus));
    }

    private interface ISubscription
    {
        Task HandleAsync(object eventData, CancellationToken ct);
    }

    private class ActionSubscription<T>(Action<T> handler, Action<Type, ISubscription> removeAction) : ISubscription, IDisposable
        where T : class
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            removeAction(typeof(T), this);
            disposed = true;
        }

        public Task HandleAsync(object eventData, CancellationToken ct)
        {
            if (eventData is T typedEvent)
            {
                handler(typedEvent);
            }

            return Task.CompletedTask;
        }
    }

    private class AsyncSubscription<T>(Func<T, Task> handler, Action<Type, ISubscription> removeAction) : ISubscription, IDisposable
        where T : class
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            removeAction(typeof(T), this);
            disposed = true;
        }

        public async Task HandleAsync(object eventData, CancellationToken ct)
        {
            if (eventData is T typedEvent)
            {
                await handler(typedEvent);
            }
        }
    }

    private class TypedSubscription
        <T, THandler>(IServiceProvider serviceProvider, Action<Type, ISubscription> removeAction) : ISubscription, IDisposable
        where T : class
        where THandler : IEventHandler<T>
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            removeAction(typeof(T), this);
            disposed = true;
        }

        public async Task HandleAsync(object eventData, CancellationToken ct)
        {
            if (eventData is T typedEvent)
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                THandler handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(typedEvent, ct);
            }
        }
    }
}
