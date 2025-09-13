namespace Warehouse.Backend.Core.Abstractions.EventBus;

public interface IEventHandler<in T>
    where T : class
{
    Task HandleAsync(T eventData, CancellationToken ct = default);
}
