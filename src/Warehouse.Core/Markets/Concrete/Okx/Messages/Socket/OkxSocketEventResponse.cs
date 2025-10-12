using System.Text.Json.Serialization;

namespace Warehouse.Core.Markets.Concrete.Okx.Messages.Socket;

public record OkxSocketEventResponse
{
    [JsonPropertyName("event")]
    public OkxEvent? Event { get; init; }
}
