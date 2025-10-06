using System.Net.WebSockets;

namespace Warehouse.Core.Infrastructure.WebSockets;

public class WebSocketMessage
{
    public string? Text { get; init; }

    public byte[]? Binary { get; init; }

    public WebSocketMessageType Type { get; init; }

    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
