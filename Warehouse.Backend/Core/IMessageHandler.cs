namespace Warehouse.Backend.Core;

public interface IMessageHandler<in T>
{
    Task<bool> CanHandleAsync(T message);

    Task HandleAsync(T message, CancellationToken cancellationToken);
}
