namespace Warehouse.Core.Infrastructure.WebSockets;

public class WebSocketError
{
    public Exception Exception { get; init; } = null!;

    public string Message { get; init; } = string.Empty;

    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
