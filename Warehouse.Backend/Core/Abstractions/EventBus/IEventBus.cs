namespace Warehouse.Backend.Core.Abstractions.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T eventData, CancellationToken ct = default)
        where T : class;

    IDisposable Subscribe<T>(Action<T> handler)
        where T : class;

    IDisposable Subscribe<T>(Func<T, Task> handler)
        where T : class;
}
