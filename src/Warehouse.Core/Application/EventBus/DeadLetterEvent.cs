namespace Warehouse.Core.Application.EventBus;

public class DeadLetterEvent
{
    public required Type OriginalEventType { get; init; }

    public required object OriginalEventData { get; init; }

    public required string Message { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
