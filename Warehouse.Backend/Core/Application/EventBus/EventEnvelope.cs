namespace Warehouse.Backend.Core.Application.EventBus;

public class EventEnvelope(object eventData, Type eventType, DateTime timestamp)
{
    public object EventData { get; } = eventData;

    public Type EventType { get; } = eventType;

    public DateTime Timestamp { get; } = timestamp;
}
