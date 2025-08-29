using System.Text.Json.Serialization;

namespace Warehouse.Backend.Markets.Okx.Messages.Socket;

public record OkxSocketEventResponse
{
    [JsonPropertyName("event")]
    public OkxEvent? Event { get; init; }
}
